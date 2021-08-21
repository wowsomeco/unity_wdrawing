using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Wowsome.Drawing {
  [RequireComponent(typeof(Mask))]
  public class WDDrawingCanvas : WDCanvasBase {
    public Action OnDone { get; set; }
    public List<Vector2> DotWorldPoints => _checkPoints.Map(rt => rt.transform.position.ToVector2());

    public RectTransform pointGroup;

    List<RectTransform> _checkPoints = new List<RectTransform>();
    Checkpoint _checker;
    Mask _mask;

    public void SetDone() {
      Disabled = true;
      OnDone?.Invoke();
    }

    public override void Reactivate() {
      base.Reactivate();

      _mask.enabled = true;
    }

    public override void InitCanvas(Texture2D textureBrush, Texture2D eraser, Camera cam, float distanceBetweenPoints = .15f) {
      base.InitCanvas(textureBrush, eraser, cam, distanceBetweenPoints);

      _mask = GetComponent<Mask>();
      // init check points
      _checkPoints = pointGroup.GetComponentsWithoutSelf<RectTransform>(true);
      _checker = new Checkpoint(_checkPoints);
      // on canvas rendered, check if done
      OnRenderCanvas += ev => {
        bool isEraser = ev.Color.IsTransparent();
        _checker.Check(ev.Pos, !isEraser);
      };

      OnEndedPainting += pos => {
        if (_checker.Done) OnDone?.Invoke();
      };

      OnActive += flag => {
        _mask.enabled = flag;
      };
    }

    public override void UpdateCanvas(float dt) {
      base.UpdateCanvas(dt);
    }
  }
}

