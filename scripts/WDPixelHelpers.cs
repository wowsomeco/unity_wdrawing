using System.Collections.Generic;
using UnityEngine;

namespace Wowsome.Drawing {
  public class Pixel {
    public Vec2Int Pos { get; private set; }
    public int Index { get; private set; }

    public Pixel(Vec2Int pos, int idx) {
      Pos = pos;
      Index = idx;
    }
  }

  public class PixelGrid {
    delegate Color32 GetColor(int idx);

    public Vec2Int Pos { get; private set; }
    public Dictionary<int, Pixel> Pixels { get; private set; }
    public Vec2Int Size { get; private set; }

    Color32[] _colors = null;
    Texture2D _texture = null;
    Rect _rect;
    Vec2Int _startPos;

    public PixelGrid(Vec2Int size, Vec2Int mid, Color32 color) {
      Pos = mid;
      Size = size;

      _rect = new Rect(mid.ToVec2(), size.ToVec2());
      _startPos = new Vec2Int(mid.X - (size.X / 2), mid.Y - (size.Y / 2));

      _texture = new Texture2D(Size.X, Size.Y);
      Clear();
    }

    public void Clear() {
      Pixels = new Dictionary<int, Pixel>();
      _colors = new Color32[Size.Xy];
    }

    public bool Stamp(Vec2Int size, Vec2Int pos, Color32[] pixels, Color32 color) {
      bool stamped = false;

      bool intersects = _rect.Intersects(new Rect(pos.ToVec2(), size.ToVec2()));
      if (intersects) {
        stamped = true;
        // get the one dimension index of the pixel first
        int theY = pos.Y - _startPos.Y;
        int theX = pos.X - _startPos.X;
        int idx = (Size.X * theY) + theX;
        // check if exists already,
        // if it doesnt then add it to the dictionary
        Pixel pixel = null;
        bool exists = Pixels.TryGetValue(idx, out pixel);
        if (!exists) {
          pixel = new Pixel(pos, idx);
          Pixels[idx] = pixel;
        }

        int totalPixel = _colors.Length;
        int pixIdx = pixel.Index;
        int i = 0;

        for (int y = 0; y < size.Y; ++y) {
          bool isLeftEdge = false;
          for (int x = 0; x < size.X; ++x) {
            Color32 pixelColor = pixels[i];
            // make sure the pixel idx isnt out of range
            bool idxInRange = pixIdx >= 0 && pixIdx < totalPixel;
            if (!idxInRange) break;
            // skip if left edge until the row (y) changes
            // skip the transparent color, only stamp the black one            
            if (!isLeftEdge && !pixelColor.IsTransparent()) {
              if (!color.IsTransparent()) {
                int alpha = pixelColor.a;
                // if not transparent, add from the cur alpha
                Color32 curColor = _colors[pixIdx];
                if (!curColor.IsTransparent()) {
                  alpha += (int)curColor.a;
                  alpha = alpha.Clamp(0, 255);
                }

                _colors[pixIdx] = color.WithAlpha((byte)alpha);
              } else {
                // make smooth edge
                int a = 255 - (int)pixelColor.a;
                a = a.Clamp(0, 5);
                Color32 curColor = _colors[pixIdx];
                if (a > 0 && !curColor.IsTransparent()) {
                  _colors[pixIdx] = curColor.WithAlpha((byte)a);
                } else {
                  _colors[pixIdx] = Ext.TransparentWhite();
                }
              }
            }

            ++pixIdx;
            ++i;

            if (!isLeftEdge) {
              isLeftEdge = (pixIdx % Size.X == 0);
              // revert prev
              if (isLeftEdge) {
                _colors[pixIdx - 1] = Ext.TransparentWhite();
              }
            }

            bool last = (x == size.X - 1);
            // shift up the index one row if last
            if (last) {
              pixIdx = pixIdx + Size.X - size.X;
              isLeftEdge = false;
            }
          }
        }
      }

      return stamped;
    }

    public Texture2D GetTexture() {
      _texture.SetPixels32(_colors);
      _texture.Apply();
      return _texture;
    }
  }

  public class Checkpoint {
    public class Point {
      public bool Done { get; private set; }

      Vector2 _pos;

      public Point(Vector2 pos, RectTransform rt) {
        _pos = pos;
      }

      public bool CheckDone(Vector2 dragPos, bool shouldDone) {
        float dist = Vector2.Distance(dragPos, _pos);
        // might want to make the leeway configurable via param later
        bool isInDistance = dist < 40f;
        if (isInDistance) {
          if (shouldDone) {
            if (!Done) Done = true;
          } else {
            Done = false;
          }
        }
        return Done;
      }
    }

    List<Point> _points = new List<Point>();

    public bool Done { get; private set; }

    public Checkpoint(List<RectTransform> points) {
      points.ForEach(p => _points.Add(new Point(p.Pos(), p)));
    }

    public bool Check(Vector2 pos, bool shouldDone) {
      Done = true;
      foreach (Point p in _points) {
        if (!p.CheckDone(pos, shouldDone)) Done = false;
      }
      return Done;
    }
  }
}