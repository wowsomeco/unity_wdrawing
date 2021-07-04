using System;
using UnityEngine;
using UnityEngine.UI;

namespace Wowsome.Drawing {
  [RequireComponent(typeof(Canvas))]
  [RequireComponent(typeof(Image))]
  public class WDCanvasBase : MonoBehaviour {
    public delegate Color32 GetColor();

    public struct RenderCanvasEv {
      public Color32 Color { get; private set; }
      public Vector2 Pos { get; private set; }

      public RenderCanvasEv(Color32 col, Vector2 pos) {
        Color = col;
        Pos = pos;
      }
    }

    public Image Img { get { return _img; } }
    public RectTransform Rt { get { return _rt; } }
    public RawImage DrawArea { get { return _drawArea; } }
    public GetColor GetPaintColor { get; set; }
    public Action OnStartPainting { get; set; }
    public Action<Vector2> OnEndedPainting { get; set; }
    public Action<Vector2> OnPainting { get; set; }
    public Action<RenderCanvasEv> OnRenderCanvas { get; set; }
    public Action<bool> OnActive { get; set; }

    public bool Disabled {
      get { return _disabled; }
      set {
        _disabled = value;
        Img.raycastTarget = !_disabled;
      }
    }

    public bool Active {
      get { return gameObject.activeSelf; }
      set {
        _drawArea.enabled = value;
        _img.enabled = value;
        _canvas.enabled = value;
        gameObject.SetActive(value);

        if (value) {
          _img.SetAlpha(0f);
        }

        OnActive?.Invoke(value);
      }
    }

    Camera _camera;
    RawImage _drawArea;
    Texture2D _brush;
    Texture2D _eraser;
    Image _img;
    RectTransform _rt;
    Canvas _canvas;
    PixelGrid _drawAreaPixels;
    Color32[] _brushPixels;
    Color32[] _eraserPixels;
    bool _firstPaint = false;
    bool _dragging = false;
    Vector2 _lastDragPos = Vector2.zero;
    float _drawLineThreshold = 8f;
    float _renderThreshold = 4f;
    Color32 _lastPaintColor;
    bool _disabled = false;

    public virtual void Reactivate() {
      _drawArea.enabled = true;
      _img.enabled = true;
      _canvas.enabled = true;
      Disabled = false;
    }

    public bool Render(Vector2 screenPos) {
      if (Paint(screenPos)) {
        Render();
        return true;
      }

      return false;
    }

    public void Clear() {
      _drawAreaPixels.Clear();
      Render();

      _firstPaint = false;
      _dragging = false;
      Resources.UnloadUnusedAssets();
    }

    public bool OnTap(Vector2 screenPos) {
      return Disabled ? false : Render(screenPos);
    }

    public virtual void OnStartSwipe(Vector2 screenPos) {
      OnTap(screenPos);
    }

    public virtual bool OnSwiping(Vector2 screenPos) {
      if (Disabled) return false;
      // dont render if outside the rect
      bool isInsideRect = RectTransformUtility.RectangleContainsScreenPoint(_rt, screenPos, _camera);
      if (!isInsideRect) return false;
      bool painted = false;
      // convert pos to anchored pos
      Vector2 pos = screenPos;
      float distance = Vector2.Distance(_lastDragPos, pos);
      // optimization, dont render anything unless the distance to prev is not too close...
      // comment it out for now
      if (distance < _renderThreshold) return false;
      // draw line logic here ... 
      if (_firstPaint && _dragging) {
        // draw line if distance is too far between delta                              
        if (distance > _drawLineThreshold) {
          float t = 0f;
          while (t < 1f) {
            // TODO: make this configurable later
            t += 0.2f;
            Vector2 p = Vector2.Lerp(pos, _lastDragPos, t);
            Paint(p);
          }
        }
      }
      // last paint
      painted = Paint(pos);
      // cache to last drag
      _lastDragPos = pos;
      if (!_dragging) {
        _dragging = true;
        // broadcast ev start painting
        OnStartPainting?.Invoke();
      }
      // apply render only if painted
      if (painted) {
        Render();
        if (!_firstPaint) _firstPaint = true;
        // check if done
        bool isEraser = _lastPaintColor.IsTransparent();
        // broadcast render canvas ev
        Vector2 localPos = screenPos.ScreenToLocalPos(_rt, _camera);
        OnRenderCanvas?.Invoke(new RenderCanvasEv(_lastPaintColor, localPos));
      }

      return painted;
    }

    public virtual void OnEndSwipe(Vector2 screenPos) {
      if (Disabled) return;
      _dragging = false;
      // broadcast ended swipe
      OnEndedPainting?.Invoke(screenPos);
      // clean up on drag ends
      Resources.UnloadUnusedAssets();
    }

    public virtual void InitCanvas(Texture2D textureBrush, Texture2D eraser, Camera cam) {
      _camera = cam;

      _img = GetComponent<Image>();
      _rt = _img.rectTransform;
      _canvas = GetComponent<Canvas>();
      // convert size to int for better performance
      Vector2 sizeOffset = new Vector2(10f, 10f);
      Vec2Int size = new Vec2Int(_rt.Size() + sizeOffset);
      // create draw area texture
      _drawArea = ComponentExt.CreateComponent<RawImage>(_rt, "DrawArea");
      _drawArea.rectTransform.Normalize().SetSize(size.ToVec2());
      _drawArea.rectTransform.SetAsFirstSibling();
      _drawArea.gameObject.SetActive(true);
      // cache the brush pixel data
      _brush = textureBrush;
      _brushPixels = _brush.GetPixels32();
      _eraser = eraser;
      _eraserPixels = _eraser.GetPixels32();
      // init the draw pixel helper
      _drawAreaPixels = new PixelGrid(size, new Vec2Int(Vector2.zero), Color.clear);
      // fill with transparent color at the beginning      
      Render();
    }

    public virtual void UpdateCanvas(float dt) { }

    void Render() {
      _drawArea.texture = _drawAreaPixels.GetTexture();
    }

    bool Paint(Vector2 screenPos) {
      Vector2 localPos = screenPos.ScreenToLocalPos(_rt, _camera);

      _lastPaintColor = GetPaintColor();
      Vec2Int brushSize = GetBrushSize(_lastPaintColor);
      Vec2Int pos = new Vec2Int(
        new Vector2(
          localPos.x - (brushSize.X / 2),
          localPos.y - (brushSize.Y / 2)
        )
      );

      OnPainting?.Invoke(screenPos);

      return _drawAreaPixels.Stamp(
        brushSize,
        pos,
        _lastPaintColor.IsTransparent() ? _eraserPixels : _brushPixels,
        _lastPaintColor
      );
    }

    Vec2Int GetBrushSize(Color32 c) {
      bool isEraser = c.IsTransparent();
      Vector2 size = new Vector2(isEraser ? _eraser.width : _brush.width, isEraser ? _eraser.height : _brush.height);
      return new Vec2Int(size);
    }
  }
}
