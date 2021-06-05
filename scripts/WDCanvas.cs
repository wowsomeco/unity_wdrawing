using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Wowsome.Chrono;
using Wowsome.Tween;

namespace Wowsome.Drawing {
  [RequireComponent(typeof(Canvas))]
  [RequireComponent(typeof(GraphicRaycaster))]
  [RequireComponent(typeof(Image))]
  [RequireComponent(typeof(Mask))]
  public class WDCanvas : MonoBehaviour {
    public delegate Color32 GetColor();

    public RectTransform PointGroup;

    List<RectTransform> _checkPoints = new List<RectTransform>();
    RawImage _drawArea;
    Texture2D _brush;
    Texture2D _eraser;
    Image _img;
    RectTransform _rt;
    Canvas _canvas;
    Mask _mask;
    PixelGrid _drawAreaPixels;
    Color32[] _brushPixels;
    Color32[] _eraserPixels;
    bool _firstPaint = false;
    bool _dragging = false;
    Vector2 _lastDragPos = Vector2.zero;
    float _drawLineThreshold = 8f;
    // float _renderThreshold = 4f;
    Checkpoint _checker;
    Color32 _lastPaintColor;
    Tweener _fader = null;
    Timer _delayDone = null;
    bool _disabled = false;

    public Image Img { get { return _img; } }
    public RectTransform Rt { get { return _rt; } }
    public RawImage DrawArea { get { return _drawArea; } }
    public GetColor OnPainting { get; set; }
    public Action OnStartPainting { get; set; }
    public Action OnEndedPainting { get; set; }
    public Action ResetDelayDone { get; set; }
    public Action OnDone { get; set; }

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
        _mask.enabled = value;
        gameObject.SetActive(value);

        if (value) {
          _img.SetAlpha(0f);
          _fader = new Tweener(Tweener.FadeIn(_img.gameObject));
          _fader.Play();
        }
      }
    }

    public List<Vector2> DotWorldPoints {
      get { return _checkPoints.Map(rt => rt.transform.position.ToVector2()); }
    }

    public void Reactivate() {
      _drawArea.enabled = true;
      _img.enabled = true;
      _canvas.enabled = true;
      _mask.enabled = true;
      Disabled = false;
      _delayDone = null;
    }

    public bool OnTap(Vector2 screenPos) {
      if (!Disabled && Paint(screenPos.ToLocalPos(_rt))) {
        _delayDone = null;
        Render();
        return true;
      }

      return false;
    }

    public void OnStartSwipe(Vector2 screenPos) {
      _delayDone = null;
    }

    public bool OnSwiping(Vector2 screenPos) {
      if (Disabled) return false;
      // dont render if outside the rect
      bool isInsideRect = RectTransformUtility.RectangleContainsScreenPoint(_rt, screenPos, null);
      if (!isInsideRect) return false;
      bool painted = false;
      // convert pos to anchored pos
      Vector2 pos = screenPos.ToLocalPos(_rt);
      float distance = Vector2.Distance(_lastDragPos, pos);
      // optimization, dont render anything unless the distance to prev is not too close...
      // comment it out for now
      // if (distance < _renderThreshold) return;
      // draw line logic here ... 
      if (_firstPaint && _dragging) {
        // draw line if distance is too far between delta                              
        if (distance > _drawLineThreshold) {
          float t = 0f;
          while (t < 1f) {
            t += 0.05f;
            Paint(Vector2.Lerp(pos, _lastDragPos, t));
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
        if (null != OnStartPainting) OnStartPainting.Invoke();
      }
      // apply render only if painted
      if (painted) {
        Render();
        if (!_firstPaint) _firstPaint = true;
        // check if done
        bool isEraser = _lastPaintColor.IsTransparent();
        _checker.Check(pos, !isEraser);
      }

      return painted;
    }

    public void OnEndSwipe(Vector2 screenPos) {
      if (Disabled) return;
      _dragging = false;
      // broadcast ended swipe
      OnEndedPainting.Invoke();
      // clean up on drag ends
      Resources.UnloadUnusedAssets();
      // broadcast done
      if (_checker.Done) {
        _delayDone = new Timer(2f);
      }
    }

    public void InitCanvas(Texture2D textureBrush, Texture2D eraser) {
      _img = GetComponent<Image>();
      _rt = _img.rectTransform;
      _canvas = GetComponent<Canvas>();
      _mask = GetComponent<Mask>();
      // convert size to int for better performance
      VecInt size = new VecInt(_rt.Size() + new Vector2(10f, 10f));
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
      // TODO: might need to tweak this as this is slow currently
      _drawAreaPixels = new PixelGrid(size, new VecInt(Vector2.zero), Color.clear);
      // fill with transparent color at the beginning      
      Render();
      // init check points
      _checkPoints = PointGroup.GetComponentsWithoutSelf<RectTransform>(true);
      _checker = new Checkpoint(_checkPoints);
      // observe reset delay
      ResetDelayDone += () => _delayDone = null;
    }

    public void UpdateCanvas(float dt) {
      if (null != _fader) { _fader.Update(dt); }

      if (null != _delayDone && !_delayDone.UpdateTimer(dt)) {
        _delayDone = null;
        SetDone();
      }
    }

    public void SetDone() {
      _delayDone = null;
      Disabled = true;
      OnDone.Invoke();
    }

    void Render() {
      if (Disabled) return;
      _drawArea.texture = _drawAreaPixels.GetTexture();
    }

    bool Paint(Vector2 draggingPos) {
      if (Disabled) return false;

      _lastPaintColor = OnPainting();
      VecInt brushSize = GetBrushSize(_lastPaintColor);
      VecInt pos = new VecInt(
        new Vector2(
          draggingPos.x - (brushSize.x / 2),
          draggingPos.y - (brushSize.y / 2)
        )
      );

      return _drawAreaPixels.Stamp(
        brushSize,
        pos,
        _lastPaintColor.IsTransparent() ? _eraserPixels : _brushPixels,
        _lastPaintColor
      );
    }

    Vector2 BrushSize(bool isEraser) {
      return new Vector2(isEraser ? _eraser.width : _brush.width, isEraser ? _eraser.height : _brush.height);
    }

    VecInt GetBrushSize(Color32 c) {
      bool isEraser = c.IsTransparent();
      Vector2 size = new Vector2(isEraser ? _eraser.width : _brush.width, isEraser ? _eraser.height : _brush.height);
      return new VecInt(size);
    }
  }
}
