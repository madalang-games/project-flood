using System.Collections;
using System.Collections.Generic;
using Game.Core.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Game.OutGame.Lobby
{
    /// <summary>
    /// Decoration layer for a chapter area (no gradient — gradient handled by HomeTabView).
    /// Contains theme-specific animated elements: grassland or ocean.
    /// </summary>
    public class ChapterBackgroundView : MonoBehaviour
    {
        private struct Particle { public RectTransform Rt; public Image Img; public float Phase; }
        private struct FishState  { public RectTransform Rt; public float Dir; public float Phase; public float Speed; }

        private const float HalfWidth = 520f;

        // ── State ──────────────────────────────────────────────────
        private ChapterBgTheme  _theme;
        private float           _height;
        private bool            _animating;
        private int             _bgThemeId;

        // Shared particles
        private readonly List<Particle>     _particles = new();

        // Grassland decorations
        private RectTransform               _sun;
        private readonly List<RectTransform>_clouds     = new();
        private readonly List<RectTransform>_grass      = new();
        private readonly List<float>        _grassPhase = new();

        // Ocean decorations
        private readonly List<(RectTransform rt, float phase, float speed, float baseY)> _waves = new();
        private readonly List<FishState>    _fishList   = new();
        private readonly List<(Image img, float phase, float period)> _rays = new();

        // ── Public API ─────────────────────────────────────────────
        public void Bind(int chapterId, int bgThemeId, float yAnchoredTop, float height)
        {
            _bgThemeId = bgThemeId;
            _theme     = ChapterBgTheme.Get(bgThemeId);
            _height    = height;

            var rt = GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot     = new Vector2(0.5f, 1f);
            rt.offsetMax = new Vector2(0f, yAnchoredTop);
            rt.offsetMin = new Vector2(0f, yAnchoredTop - height);

            CreateParticles();
            CreateDecorations(bgThemeId);

            if (gameObject.activeInHierarchy)
                StartCoroutine(MasterLoop());
        }

        // ── Lifecycle ──────────────────────────────────────────────
        private void OnEnable()
        {
            if (_height > 0f && !_animating)
                StartCoroutine(MasterLoop());
        }

        private void OnDisable()
        {
            _animating = false;
            StopAllCoroutines();
        }

        // ── Creation: particles ───────────────────────────────────
        private void CreateParticles()
        {
            int   count = _theme.ParticleCount;
            float minSz = _theme.ParticleSize * 0.6f;
            float maxSz = _theme.ParticleSize * 1.6f;

            for (int i = 0; i < count; i++)
            {
                float sz = Random.Range(minSz, maxSz);
                var   go = new GameObject($"P{i}", typeof(Image));
                go.transform.SetParent(transform, false);
                var img = go.GetComponent<Image>();
                img.color         = new Color(_theme.ParticleColor.r, _theme.ParticleColor.g, _theme.ParticleColor.b, 0f);
                img.raycastTarget = false;
                var pRt = go.GetComponent<RectTransform>();
                pRt.anchorMin = pRt.anchorMax = new Vector2(0.5f, 1f);
                pRt.pivot     = new Vector2(0.5f, 0.5f);
                pRt.sizeDelta = new Vector2(sz, sz);
                pRt.anchoredPosition = RandomPos();
                _particles.Add(new Particle { Rt = pRt, Img = img, Phase = Random.Range(0f, Mathf.PI * 2f) });
            }
        }

        // ── Creation: theme dispatch ───────────────────────────────
        private void CreateDecorations(int id)
        {
            if (id == 1) { CreateSun(); CreateClouds(3); CreateGrass(20); }
            if (id == 2) { CreateWaves(3); CreateFish(2); CreateRays(5); }
        }

        // ── Grassland ─────────────────────────────────────────────
        private void CreateSun()
        {
            float sunY = -_height * 0.10f;
            float sunX = HalfWidth * 0.55f;

            // Glow rings (back layers first)
            for (int r = 3; r >= 1; r--)
            {
                float sz = 72f + r * 36f;
                MakeImg(transform, new Vector2(sunX, sunY), new Vector2(sz, sz),
                    new Color(1f, 0.85f, 0.2f, 0.06f * (4 - r)));
            }

            // Sun body
            var sunImg = MakeImg(transform, new Vector2(sunX, sunY), new Vector2(66f, 66f),
                new Color(1f, 0.90f, 0.25f, 0.92f));
            _sun = sunImg.GetComponent<RectTransform>();

            // Pixel-art rays: 8 thin rects at 45° intervals
            for (int i = 0; i < 8; i++)
            {
                float angle = i * 45f;
                float rad   = angle * Mathf.Deg2Rad;
                float dist  = 52f;
                var   rPos  = new Vector2(sunX + Mathf.Cos(rad) * dist, sunY + Mathf.Sin(rad) * dist);
                var   ray   = MakeImg(transform, rPos, new Vector2(6f, 22f),
                    new Color(1f, 0.88f, 0.3f, 0.65f));
                ray.GetComponent<RectTransform>().localRotation = Quaternion.Euler(0f, 0f, angle + 90f);
            }
        }

        private void CreateClouds(int count)
        {
            float[] ys     = { -_height * 0.06f, -_height * 0.14f, -_height * 0.22f };
            float[] xs     = { -HalfWidth * 0.3f, HalfWidth * 0.15f, -HalfWidth * 0.6f };
            float[] speeds = { 14f, 9f, 18f };
            float[] widths = { 220f, 180f, 140f };

            for (int i = 0; i < count; i++)
            {
                var root = new GameObject($"Cloud{i}", typeof(RectTransform));
                root.transform.SetParent(transform, false);
                var cRt = root.GetComponent<RectTransform>();
                cRt.anchorMin = cRt.anchorMax = new Vector2(0.5f, 1f);
                cRt.pivot     = new Vector2(0.5f, 0.5f);
                cRt.sizeDelta = Vector2.one;
                cRt.anchoredPosition = new Vector2(xs[i], ys[i]);

                // Build cloud from overlapping rects
                float w = widths[i];
                float h = w * 0.38f;
                Color c = new Color(1f, 1f, 1f, 0.60f);
                BuildCloudPart(root.transform, new Vector2(0, 0),           new Vector2(w,        h * 0.65f), c);
                BuildCloudPart(root.transform, new Vector2(-w * 0.18f, h * 0.18f), new Vector2(w * 0.52f, h * 0.9f),  c);
                BuildCloudPart(root.transform, new Vector2( w * 0.12f, h * 0.10f), new Vector2(w * 0.48f, h),         c);

                _clouds.Add(cRt);
            }

            // Store per-cloud drift speed baked into the list index
            // (speeds stored inline in MasterLoop via cloud index)
            _ = speeds; // accessed by index in loop
        }

        private void BuildCloudPart(Transform parent, Vector2 pos, Vector2 size, Color color)
        {
            var img = MakeImg(parent, pos, size, color);
            img.GetComponent<RectTransform>().anchorMin =
            img.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 0.5f);
        }

        private void CreateGrass(int count)
        {
            float bottomY = -_height + 20f;
            float span    = HalfWidth * 1.7f;

            for (int i = 0; i < count; i++)
            {
                float h = Random.Range(26f, 52f);
                float x = -HalfWidth * 0.85f + i * (span / count) + Random.Range(-10f, 10f);
                float g = Random.Range(0.28f, 0.45f);
                var blade = MakeImg(transform, new Vector2(x, bottomY),
                    new Vector2(Random.Range(4f, 8f), h),
                    new Color(0.18f, g, 0.12f, 0.88f));
                var bRt = blade.GetComponent<RectTransform>();
                bRt.pivot = new Vector2(0.5f, 0f); // rotate from base
                _grass.Add(bRt);
                _grassPhase.Add(Random.Range(0f, Mathf.PI * 2f));
            }
        }

        // ── Ocean ─────────────────────────────────────────────────
        private void CreateWaves(int count)
        {
            for (int i = 0; i < count; i++)
            {
                float baseY = -_height * (0.08f + i * 0.10f);
                var go = new GameObject($"Wave{i}", typeof(Image));
                go.transform.SetParent(transform, false);
                var img = go.GetComponent<Image>();
                img.color = new Color(0.55f, 0.88f, 0.98f, 0.30f - i * 0.06f);
                img.raycastTarget = false;
                var wRt = go.GetComponent<RectTransform>();
                wRt.anchorMin = new Vector2(0f, 1f);
                wRt.anchorMax = new Vector2(1f, 1f);
                wRt.pivot     = new Vector2(0.5f, 0.5f);
                wRt.sizeDelta = new Vector2(0f, 10f + i * 6f);
                wRt.anchoredPosition = new Vector2(0f, baseY);
                _waves.Add((wRt, i * 1.2f, 1.4f + i * 0.5f, baseY));
            }
        }

        private void CreateFish(int count)
        {
            float[] ys     = { -_height * 0.30f, -_height * 0.55f };
            float[] speeds = { 38f, 25f };

            for (int i = 0; i < count; i++)
            {
                float startX = Random.Range(-HalfWidth * 0.6f, HalfWidth * 0.6f);
                var root = new GameObject($"Fish{i}", typeof(RectTransform));
                root.transform.SetParent(transform, false);
                var fRt = root.GetComponent<RectTransform>();
                fRt.anchorMin = fRt.anchorMax = new Vector2(0.5f, 1f);
                fRt.pivot     = new Vector2(0.5f, 0.5f);
                fRt.sizeDelta = Vector2.one;
                fRt.anchoredPosition = new Vector2(startX, ys[i]);

                Color fc = new Color(0.25f, 0.55f, 0.82f, 0.70f);
                // Body
                MakeImgLocal(root.transform, new Vector2(3f, 0), new Vector2(30f, 13f), fc);
                // Tail
                MakeImgLocal(root.transform, new Vector2(-16f, 0), new Vector2(11f, 16f),
                    new Color(fc.r, fc.g, fc.b, fc.a * 0.8f));
                // Eye
                MakeImgLocal(root.transform, new Vector2(11f, 2f), new Vector2(4f, 4f),
                    new Color(0.9f, 0.95f, 1f, 0.9f));

                _fishList.Add(new FishState
                {
                    Rt    = fRt,
                    Dir   = i % 2 == 0 ? 1f : -1f,
                    Phase = Random.Range(0f, Mathf.PI * 2f),
                    Speed = speeds[i],
                });
            }
        }

        private void CreateRays(int count)
        {
            for (int i = 0; i < count; i++)
            {
                float x = Random.Range(-HalfWidth * 0.55f, HalfWidth * 0.55f);
                float y = -Random.Range(_height * 0.04f, _height * 0.55f);
                var ray = MakeImg(transform, new Vector2(x, y),
                    new Vector2(Random.Range(10f, 22f), Random.Range(140f, 280f)),
                    new Color(0.72f, 0.95f, 1f, 0f));
                ray.GetComponent<RectTransform>().localRotation =
                    Quaternion.Euler(0f, 0f, Random.Range(-22f, 22f));
                _rays.Add((ray, Random.Range(0f, Mathf.PI * 2f), Random.Range(3f, 7f)));
            }
        }

        // ── Master animation loop ─────────────────────────────────
        private IEnumerator MasterLoop()
        {
            _animating = true;
            ScatterParticles();

            while (_animating)
            {
                float dt = Time.deltaTime;
                float t  = Time.time;
                UpdateParticles(dt, t);
                if (_bgThemeId == 1) UpdateGrassland(dt, t);
                if (_bgThemeId == 2) UpdateOcean(dt, t);
                yield return null;
            }
        }

        private void ScatterParticles()
        {
            for (int i = 0; i < _particles.Count; i++)
            {
                var pos = _particles[i].Rt.anchoredPosition;
                pos.y = -Random.Range(0f, _height);
                _particles[i].Rt.anchoredPosition = pos;
            }
        }

        private void UpdateParticles(float dt, float t)
        {
            for (int i = 0; i < _particles.Count; i++)
            {
                var p   = _particles[i];
                var pos = p.Rt.anchoredPosition;

                pos.y += _theme.ParticleSpeed * dt;
                pos.x += Mathf.Sin(t * 0.65f + p.Phase) * 18f * dt;
                pos.x  = Mathf.Clamp(pos.x, -HalfWidth * 0.44f, HalfWidth * 0.44f);
                p.Rt.anchoredPosition = pos;

                if (pos.y > 0f)
                {
                    p.Rt.anchoredPosition = new Vector2(
                        Random.Range(-HalfWidth * 0.4f, HalfWidth * 0.4f), -_height);
                    p.Img.color = new Color(_theme.ParticleColor.r, _theme.ParticleColor.g,
                        _theme.ParticleColor.b, 0f);
                    continue;
                }
                float norm  = Mathf.Clamp01(-pos.y / _height);
                float alpha = _theme.ParticleColor.a * Mathf.Sin(norm * Mathf.PI);
                p.Img.color = new Color(_theme.ParticleColor.r, _theme.ParticleColor.g,
                    _theme.ParticleColor.b, alpha);
            }
        }

        private void UpdateGrassland(float dt, float t)
        {
            // Sun gentle pulse
            if (_sun != null)
            {
                float s = 1f + 0.04f * Mathf.Sin(t * 1.1f);
                _sun.localScale = new Vector3(s, s, 1f);
            }

            // Cloud drift
            float[] cloudSpeeds = { 14f, 9f, 18f };
            for (int i = 0; i < _clouds.Count; i++)
            {
                var pos = _clouds[i].anchoredPosition;
                pos.x += cloudSpeeds[i % cloudSpeeds.Length] * dt;
                if (pos.x > HalfWidth + 160f) pos.x = -HalfWidth - 160f;
                _clouds[i].anchoredPosition = pos;
            }

            // Grass sway
            for (int i = 0; i < _grass.Count; i++)
            {
                float angle = Mathf.Sin(t * 2.1f + _grassPhase[i]) * 11f;
                _grass[i].localRotation = Quaternion.Euler(0f, 0f, angle);
            }
        }

        private void UpdateOcean(float dt, float t)
        {
            // Wave undulation
            for (int i = 0; i < _waves.Count; i++)
            {
                var (rt, phase, speed, baseY) = _waves[i];
                float newY = baseY + Mathf.Sin(t * speed + phase) * 14f;
                rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, newY);
            }

            // Fish swim
            for (int i = 0; i < _fishList.Count; i++)
            {
                var f   = _fishList[i];
                var pos = f.Rt.anchoredPosition;
                pos.x += f.Speed * f.Dir * dt;
                pos.y += Mathf.Sin(t * 1.8f + f.Phase) * 0.4f * dt;

                if (pos.x > HalfWidth * 0.85f)
                {
                    _fishList[i] = new FishState { Rt = f.Rt, Dir = -1f, Phase = f.Phase, Speed = f.Speed };
                    f.Rt.localScale = new Vector3(-1f, 1f, 1f);
                }
                else if (pos.x < -HalfWidth * 0.85f)
                {
                    _fishList[i] = new FishState { Rt = f.Rt, Dir = 1f, Phase = f.Phase, Speed = f.Speed };
                    f.Rt.localScale = Vector3.one;
                }

                f.Rt.anchoredPosition = pos;
            }

            // Caustic ray pulse
            for (int i = 0; i < _rays.Count; i++)
            {
                var (img, phase, period) = _rays[i];
                float alpha = (0.5f + 0.5f * Mathf.Sin(t * (Mathf.PI * 2f / period) + phase)) * 0.13f;
                img.color = new Color(img.color.r, img.color.g, img.color.b, alpha);
            }
        }

        // ── Helpers ────────────────────────────────────────────────
        // Anchor (0.5, 1) – for items positioned within the chapter area
        private static Image MakeImg(Transform parent, Vector2 pos, Vector2 size, Color color)
        {
            var go = new GameObject("_", typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.color = color; img.raycastTarget = false;
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
            return img;
        }

        // Anchor (0.5, 0.5) – for cloud parts / fish parts (relative to parent center)
        private static Image MakeImgLocal(Transform parent, Vector2 pos, Vector2 size, Color color)
        {
            var go = new GameObject("_", typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.color = color; img.raycastTarget = false;
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
            return img;
        }

        private Vector2 RandomPos() => new(
            Random.Range(-HalfWidth * 0.4f, HalfWidth * 0.4f),
            -Random.Range(0f, _height));
    }
}
