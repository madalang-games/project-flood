using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Core.UI
{
    [RequireComponent(typeof(CanvasRenderer))]
    public class UILineStrip : MaskableGraphic
    {
        public float lineWidth = 12f;

        private readonly List<Vector2> _pts = new List<Vector2>();

        public void SetPoints(IList<Vector2> pts)
        {
            _pts.Clear();
            foreach (var p in pts) _pts.Add(p);
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (_pts.Count < 2) return;

            float hw = lineWidth * 0.5f;
            for (int i = 0; i < _pts.Count - 1; i++)
            {
                Vector2 a = _pts[i];
                Vector2 b = _pts[i + 1];
                Vector2 d = (b - a).normalized;
                Vector2 n = new Vector2(-d.y, d.x) * hw;

                int k = vh.currentVertCount;
                vh.AddVert(a - n, color, Vector2.zero);
                vh.AddVert(a + n, color, Vector2.up);
                vh.AddVert(b + n, color, Vector2.one);
                vh.AddVert(b - n, color, Vector2.right);
                vh.AddTriangle(k,     k + 1, k + 2);
                vh.AddTriangle(k,     k + 2, k + 3);
            }
        }
    }
}
