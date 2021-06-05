using System.Collections.Generic;
using UnityEngine;

namespace Wowsome.Drawing {
  public static class Ext {
    public static Color32 TransparentWhite() {
      return new Color32(255, 255, 255, 0);
    }

    public static Color32 White() {
      return new Color32(255, 255, 255, 255);
    }

    public static Color32 Purple() {
      // #a29bfe
      return new Color32(162, 155, 254, 255);
    }

    public static Color32 Blue() {
      // #74b9ff
      return new Color32(116, 185, 255, 255);
    }

    public static Color32 Green() {
      // #81ecec
      return new Color32(129, 236, 236, 255);
    }

    public static Color32 Red() {
      // #fab1a0
      return new Color32(250, 177, 160, 255);
    }

    public static Color32 Yellow() {
      // #ffeaa7
      return new Color32(255, 234, 167, 255);
    }

    public static Color32 Rainbow(int idx) {
      Color32[] seeds = new Color32[] { Blue(), Green(), Yellow() };
      return seeds[idx];
    }

    public static Color32 WithAlpha(this Color32 color, byte a) {
      return new Color32(color.r, color.g, color.b, a);
    }

    public static bool IsTransparent(this Color32 c) {
      return c.a == 0;
    }

    public static bool LessThan(this Color32 c, byte number) {
      return c.r < number && c.g < number && c.b < number;
    }

    public static byte Subtract(this byte b, int v) {
      int n = b - v < 0 ? b + v : b - v;
      return (byte)n;
    }

    public static Color32 Lighten(this Color32 color, int alpha) {
      if (alpha > 250) return color;

      float factor = (float)(alpha) / 255f;
      factor += 0.6f;
      factor = factor.Clamp(0.1f, 1f);

      float r = (color.r / factor).Clamp(0f, 255f);
      float g = (color.g / factor).Clamp(0f, 255f);
      float b = (color.b / factor).Clamp(0f, 255f);

      return new Color32((byte)r, (byte)g, (byte)b, color.a);
    }

    public static Color32 RandomizeAlpha(this Color32 c, int from = 0, int to = 255) {
      return new Color32(
        c.r,
        c.g,
        c.b,
        (byte)Random.Range(from, to)
      );
    }

    public static Vector2 ToLocalPos(this Vector2 screenPos, RectTransform parent, Camera camera = null) {
      Vector2 pos;
      RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screenPos, camera, out pos);
      return pos;
    }
  }

  public struct VecInt {
    Vector2 _vector;

    public int x { get { return Mathf.FloorToInt(_vector.x); } }
    public int xHalf { get { return x / 2; } }
    public int y { get { return Mathf.FloorToInt(_vector.y); } }
    public int yHalf { get { return y / 2; } }
    public int xy { get { return x * y; } }
    public int Sum { get { return x + y; } }
    public VecInt Half { get { return new VecInt(xHalf, yHalf); } }

    public VecInt(Vector2 v) {
      _vector = v;
    }

    public VecInt(int x, int y) {
      _vector = new Vector2(x, y);
    }

    public override string ToString() {
      return x + "," + y;
    }

    public override int GetHashCode() {
      return (x << 16) | y;
    }

    public override bool Equals(object obj) {
      if (!(obj is VecInt))
        return false;

      VecInt other = (VecInt)obj;
      return x == other.x && y == other.y;
    }

    public Vector2 ToVec2() {
      return new Vector2(x, y);
    }
  }

  public class Pixel {
    // public Color32 Color { get; set; }
    public VecInt Pos { get; private set; }
    public int Index { get; private set; }

    public Pixel(/*Color32 color,*/ VecInt pos, int idx) {
      // Color = color; 
      Pos = pos;
      Index = idx;
    }
  }

  public class PixelGrid {
    delegate Color32 GetColor(int idx);

    public VecInt Pos { get; private set; }
    public Dictionary<VecInt, Pixel> Pixels { get; private set; }
    public VecInt Size { get; private set; }

    Color32[] _colors = null;
    Texture2D _texture = null;
    // Rect _rect;

    public PixelGrid(VecInt size, VecInt mid, Color32 color) {
      // _rect = new Rect(mid.ToVec2(), size.ToVec2());
      GeneratePixels(size, mid, idx => color);
    }

    public bool Stamp(VecInt size, VecInt pos, Color32[] pixels, Color32 color) {
      bool stamped = false;

      /*
      // TODO: optimization
      // 1. check if pos is within the rectangle of the texture
      // 2. if yes and Pixels dist does not contain contain the pos key yet, then add to the dictionary.
      // if (intersects) 
      Rect r = new Rect(pos.ToVec2(), size.ToVec2());
      bool intersects = _rect.Intersects(r);
      if (intersects) {
        Pixel pixel = null;
        bool exists = Pixels.TryGetValue(pos, out pixel);
        if (!exists) {
          
          Pixels[pos] = new Pixel(pos, idx);
        }
      }
      */

      Pixel pixel = null;
      // get the collide pos first
      // then iterate over the sides according to the stamp pixel size.      
      if (Pixels.TryGetValue(pos, out pixel)) {
        stamped = true;

        int totalPixel = Pixels.Count;
        int pixIdx = pixel.Index;
        int i = 0;

        for (int y = 0; y < size.y; ++y) {
          bool isLeftEdge = false;
          for (int x = 0; x < size.x; ++x) {
            Color32 pixelColor = pixels[i];
            // make sure the pixel idx isnt out of range
            bool idxInRange = pixIdx < totalPixel;
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
              isLeftEdge = (pixIdx % Size.x == 0);
              // revert prev
              if (isLeftEdge) {
                _colors[pixIdx - 1] = Ext.TransparentWhite();
              }
            }

            bool last = (x == size.x - 1);
            // shift up the index one row if last
            if (last) {
              pixIdx = pixIdx + Size.x - size.x;
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

    void GeneratePixels(VecInt size, VecInt mid, GetColor getColor) {
      Pos = mid;
      Pixels = new Dictionary<VecInt, Pixel>();
      Size = size;

      VecInt startPos = new VecInt(mid.x - (size.x / 2), mid.y - (size.y / 2));

      _texture = new Texture2D(Size.x, Size.y);
      // this will generate transparent texture already.
      // no need to iterate over at the bottom.
      _colors = new Color32[Size.xy];

      int i = 0;
      // FIXME: this is terribly slow.
      // need to find a better way to speed this up
      for (int y = 0; y < size.y; ++y) {
        for (int x = 0; x < size.x; ++x) {
          VecInt pixelPos = new VecInt(startPos.x + x, startPos.y + y);
          Pixels[pixelPos] = new Pixel(pixelPos, i);
          ++i;
        }
      }
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