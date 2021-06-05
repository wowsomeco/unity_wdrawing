using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Wowsome.Drawing {
  using EU = Wowsome.EditorUtils;

  [CustomEditor(typeof(WDCanvas))]
  public class WDCanvasEditor : Editor {
    int _pointNumber = 5;
    Vector2 _pointSize = Vector2.one * 15f;
    float _radiusOffset = -10f;
    float _divider = 2f;

    public override void OnInspectorGUI() {
      DrawDefaultInspector();

      WDCanvas tgt = (WDCanvas)target;

      _pointNumber = EditorGUILayout.IntField("Number of points", _pointNumber);
      _pointSize = EditorGUILayout.Vector2Field("Point Size", _pointSize);
      _radiusOffset = EditorGUILayout.FloatField("Radius Offset", _radiusOffset);
      _divider = EditorGUILayout.FloatField("Image Size Divider", _divider);

      EU.Btn("Generate Points", () => {
        if (tgt.PointGroup == null) {
          var pg = ComponentExt.CreateComponent<RectTransform>(tgt.transform, "PointGroup");
          pg.Normalize().Stretch();
          tgt.PointGroup = pg;
        }

        ClearPoints(tgt);

        Vector2 groupSize = tgt.PointGroup.Size();
        Vector2[] positions = new Vector2[_pointNumber];
        float deltaAngle = 360f / _pointNumber;
        float curAngle = 0f;
        float radius = ((groupSize.x + groupSize.y) / 4f) + _radiusOffset;
        for (int i = 0; i < positions.Length; ++i) {
          Vector2 pos = new Vector2(Mathf.Cos(curAngle * Mathf.Deg2Rad), Mathf.Sin(curAngle * Mathf.Deg2Rad));
          pos *= radius;
          curAngle += deltaAngle;
          positions[i] = pos;

          GameObject go = new GameObject("point" + i);
          go.transform.SetParent(tgt.PointGroup, false);
          Image img = go.AddComponent<Image>();
          img.SetColor(Color.magenta);
          RectTransform rt = img.rectTransform;
          rt.Normalize();
          rt.SetSize(_pointSize);
          rt.SetPos(pos);
        }

        EU.SetSceneDirty();
      });

      EU.VPadding(() => {
        EU.Btn("Image Native Size/" + _divider, () => {
          var img = tgt.GetComponent<Image>();
          img.SetNativeSize();
          img.rectTransform.DivideSize(_divider);
          EU.SetSceneDirty();
        });
      });
    }

    void ClearPoints(WDCanvas tgt) {
      tgt.PointGroup.gameObject.DestroyChildren();
      EU.SetSceneDirty();
    }
  }
}

