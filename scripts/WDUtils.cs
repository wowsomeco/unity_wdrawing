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
  }

  public class MultiColorLerper {
    Color[] paintColors;
    float _t = 0f;
    float _counter = 0.01f;
    int _cur = 0;

    public MultiColorLerper(Color[] colors) {
      paintColors = colors;
    }

    public Color Lerp() {
      // TODO: this is super ugly... refactor it later
      _t += _counter;
      if (_t > 1f) {
        _cur++;
        _t = 0f;
      } else if (_t < 0f) {
        _cur--;
        _t = 1f;
      }

      if (_cur == paintColors.Length) {
        _cur = paintColors.Length - 1;
        _t = 1f;
        _counter = -_counter;
      } else if (_cur < 0) {
        _cur = 0;
        _t = 0f;
        _counter = -_counter;
      }

      Color dest = _cur == paintColors.Length - 1 ? paintColors[_cur - 1] : paintColors[_cur + 1];
      return Color.Lerp(paintColors[_cur], dest, _t);
    }
  }
}