using System.Collections;
using System.Collections.Generic;
using Game.Core.UI;
using UnityEngine;

namespace Game.InGame.View
{
    // World Space SpriteRenderer background for InGame scene.
    // Attach to any GameObject in the InGame scene. Camera.main must be set.
    public class InGameSceneBackgroundView : MonoBehaviour
    {
        private const int SortOrder = -100;

        private readonly List<GameObject> _runtimeObjects = new();
        private bool _animating;

        private struct Sparkle
        {
            public SpriteRenderer Sr;
            public float BaseAlpha;
            public float Phase;
            public float Period;
        }
        private readonly List<Sparkle> _sparkles = new();

        // ── Public API ───────────────────────────────────────────────

        public void Bind(int bgThemeId)
        {
            transform.position = Vector3.zero;
            transform.localScale = Vector3.one;
            ClearRuntimeObjects();
            var palette = SceneBgPalette.Get(bgThemeId, BackgroundMode.Night);
            var cam     = Camera.main;
            if (cam == null) return;

            float halfH  = cam.orthographicSize;
            float halfW  = halfH * cam.aspect;
            float worldW = halfW * 2f + 0.2f;
            float worldH = halfH * 2f + 0.2f;

            BuildGradient(palette, worldW, worldH);
            BuildSparkles(palette, halfW, halfH);

            if (gameObject.activeInHierarchy)
                StartCoroutine(SparkleLoop());
        }

        // ── Lifecycle ────────────────────────────────────────────────

        private void OnDisable()
        {
            _animating = false;
            StopAllCoroutines();
        }

        // ── Build ────────────────────────────────────────────────────

        private void BuildGradient(SceneBgPalette palette, float worldW, float worldH)
        {
            const int texH = 16;
            var tex = new Texture2D(1, texH, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode   = TextureWrapMode.Clamp
            };
            for (int y = 0; y < texH; y++)
                tex.SetPixel(0, y, Color.Lerp(palette.SkyBottom, palette.SkyTop, y / (float)(texH - 1)));
            tex.Apply();

            // ppu=1 → sprite = 1×texH world units; scale to fill worldW×worldH
            var sprite = Sprite.Create(tex, new Rect(0, 0, 1, texH), new Vector2(0.5f, 0.5f), 1f);

            var go = CreateRuntimeObject("BgGradient");
            go.transform.localPosition = Vector3.zero;
            go.transform.localScale    = new Vector3(worldW, worldH / texH, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite       = sprite;
            sr.sortingOrder = SortOrder;
        }

        private void BuildSparkles(SceneBgPalette palette, float halfW, float halfH)
        {
            var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            var dot = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);

            for (int i = 0; i < palette.ParticleCount; i++)
            {
                float x   = Random.Range(-halfW * 0.88f, halfW * 0.88f);
                float y   = Random.Range(-halfH * 0.88f, halfH * 0.88f);
                float sz  = Random.Range(0.02f, 0.06f);

                var go = CreateRuntimeObject($"Sp{i}");
                go.transform.localPosition = new Vector3(x, y, 0f);
                go.transform.localScale    = new Vector3(sz, sz, 1f);

                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite       = dot;
                sr.color        = new Color(palette.ParticleColor.r, palette.ParticleColor.g,
                    palette.ParticleColor.b, palette.ParticleColor.a);
                sr.sortingOrder = SortOrder + 1;

                _sparkles.Add(new Sparkle
                {
                    Sr        = sr,
                    BaseAlpha = palette.ParticleColor.a,
                    Phase     = Random.Range(0f, Mathf.PI * 2f),
                    Period    = Random.Range(1.5f, 5f)
                });
            }
        }

        // ── Animation ────────────────────────────────────────────────

        private IEnumerator SparkleLoop()
        {
            _animating = true;
            var wait   = new WaitForSeconds(1f / 8f);
            while (_animating)
            {
                float t = Time.time;
                for (int i = 0; i < _sparkles.Count; i++)
                {
                    var s     = _sparkles[i];
                    float raw = 0.5f + 0.5f * Mathf.Sin(t * (Mathf.PI * 2f / s.Period) + s.Phase);
                    var c     = s.Sr.color;
                    s.Sr.color = new Color(c.r, c.g, c.b, raw * s.BaseAlpha);
                }
                yield return wait;
            }
        }

        // ── Cleanup ──────────────────────────────────────────────────

        private GameObject CreateRuntimeObject(string objName)
        {
            var go = new GameObject(objName);
            go.transform.SetParent(transform, false);
            _runtimeObjects.Add(go);
            return go;
        }

        private void ClearRuntimeObjects()
        {
            _sparkles.Clear();
            foreach (var go in _runtimeObjects)
            {
                if (go == null) continue;
                if (Application.isPlaying) Destroy(go);
                else DestroyImmediate(go);
            }
            _runtimeObjects.Clear();
        }
    }
}
