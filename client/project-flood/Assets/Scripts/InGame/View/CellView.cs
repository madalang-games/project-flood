using Game.InGame.Board;
using ProjectFlood.Contracts.GameTypes;
using System.Collections;
using UnityEngine;

namespace Game.InGame.View
{
    public class CellView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _baseRenderer;
        [SerializeField] private SpriteRenderer _protectorOverlay;
        [SerializeField] private GameObject _coreIndicator;

        [SerializeField] private Sprite _basicSprite;
        [SerializeField] private Sprite _obstacleSprite;
        [SerializeField] private Sprite _protectorSprite1;
        [SerializeField] private Sprite _protectorSprite2;

        private const float ColorBoost = 1.25f;

        private Vector3 _baseScale;
        private Color _baseColor;

        private void Awake()
        {
            _baseScale = transform.localScale;
        }

        public void Init(float cellSize)
        {
            var reference = _basicSprite != null ? _basicSprite : (_baseRenderer != null ? _baseRenderer.sprite : null);
            if (reference != null)
            {
                Vector2 spriteSize = reference.bounds.size;
                float s = cellSize / Mathf.Max(spriteSize.x, spriteSize.y);
                _baseScale = new Vector3(s, s, 1f);
            }
            else
            {
                _baseScale = Vector3.one * cellSize;
            }
            transform.localScale = _baseScale;
        }

        public void SetData(CellData? data, Color cellColor)
        {
            if (data == null)
            {
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);
            var cell = data.Value;

            _baseRenderer.sprite = cell.cell_type == CellType.Obstacle ? _obstacleSprite : _basicSprite;
            _baseRenderer.color = cell.cell_type == CellType.Obstacle ? Color.white : cellColor;
            _baseColor = _baseRenderer.color;
            transform.localScale = _baseScale;
            SetRenderersAlpha(1f);

            if (_protectorOverlay != null)
            {
                bool hasProtector = cell.protector_strength > 0;
                _protectorOverlay.gameObject.SetActive(hasProtector);
                if (hasProtector)
                    _protectorOverlay.sprite = cell.protector_strength == 2 ? _protectorSprite2 : _protectorSprite1;
            }

            if (_coreIndicator != null)
                _coreIndicator.SetActive(cell.is_core);
        }

        public IEnumerator PlayTapFeedback(float duration)
        {
            if (!gameObject.activeSelf) yield break;

            float half = duration * 0.5f;
            Color flashColor = Boost(_baseColor, ColorBoost);

            for (float t = 0f; t < half; t += Time.deltaTime)
            {
                float p = EaseOutBack(t / half);
                transform.localScale = Vector3.LerpUnclamped(_baseScale, _baseScale * 1.14f, p);
                _baseRenderer.color = Color.Lerp(_baseColor, flashColor, p);
                yield return null;
            }

            for (float t = 0f; t < half; t += Time.deltaTime)
            {
                float p = EaseOutCubic(t / half);
                transform.localScale = Vector3.LerpUnclamped(_baseScale * 1.14f, _baseScale, p);
                _baseRenderer.color = Color.Lerp(flashColor, _baseColor, p);
                yield return null;
            }

            transform.localScale = _baseScale;
            _baseRenderer.color = _baseColor;
        }

        public IEnumerator PlayGroupPulse(float delay, float duration)
        {
            if (!gameObject.activeSelf) yield break;
            if (delay > 0f) yield return new WaitForSeconds(delay);

            Vector3 peakScale = _baseScale * 1.08f;
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                float wave = Mathf.Sin((t / duration) * Mathf.PI);
                transform.localScale = Vector3.LerpUnclamped(_baseScale, peakScale, wave);
                yield return null;
            }

            transform.localScale = _baseScale;
        }

        public IEnumerator PlayRemove(float duration, int burstCount)
        {
            if (!gameObject.activeSelf) yield break;

            SpawnBurst(burstCount, duration * 0.85f);
            Vector3 peakScale = _baseScale * 1.22f;

            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                float p = Mathf.Clamp01(t / duration);
                transform.localScale = p < 0.22f
                    ? Vector3.LerpUnclamped(_baseScale, peakScale, EaseOutBack(p / 0.22f))
                    : Vector3.LerpUnclamped(peakScale, _baseScale * 0.08f, EaseInCubic((p - 0.22f) / 0.78f));
                SetRenderersAlpha(1f - EaseInCubic(p));
                yield return null;
            }

            SetRenderersAlpha(0f);
            transform.localScale = _baseScale;
        }

        public IEnumerator PlayProtectorHit(float duration)
        {
            if (!gameObject.activeSelf) yield break;

            Vector3 origin = transform.localPosition;
            Color hitColor = Color.Lerp(_baseColor, Color.white, 0.7f);
            float strength = 0.055f;

            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                float p = Mathf.Clamp01(t / duration);
                float shake = Mathf.Sin(p * Mathf.PI * 6f) * (1f - p) * strength;
                transform.localPosition = origin + new Vector3(shake, 0f, 0f);
                _baseRenderer.color = Color.Lerp(hitColor, _baseColor, EaseOutCubic(p));
                yield return null;
            }

            transform.localPosition = origin;
            _baseRenderer.color = _baseColor;
        }

        public IEnumerator PlayDrop(Vector3 from, Vector3 to, float delay, float duration)
        {
            if (!gameObject.activeSelf) yield break;
            if (delay > 0f) yield return new WaitForSeconds(delay);

            transform.localPosition = from;
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                float p = EaseOutBack(Mathf.Clamp01(t / duration));
                transform.localPosition = Vector3.LerpUnclamped(from, to, p);
                yield return null;
            }

            transform.localPosition = to;
            yield return PlayLandingSquash(0.11f);
        }

        private IEnumerator PlayLandingSquash(float duration)
        {
            Vector3 squash = new Vector3(_baseScale.x * 1.08f, _baseScale.y * 0.9f, _baseScale.z);

            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                float wave = Mathf.Sin((t / duration) * Mathf.PI);
                transform.localScale = Vector3.LerpUnclamped(_baseScale, squash, wave);
                yield return null;
            }

            transform.localScale = _baseScale;
        }

        private void SpawnBurst(int count, float duration)
        {
            if (_baseRenderer == null || _baseRenderer.sprite == null || count <= 0) return;

            for (int i = 0; i < count; i++)
            {
                float angle = (Mathf.PI * 2f * i) / count;
                Vector3 direction = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
                var dot = new GameObject("CellBurst");
                dot.transform.SetParent(transform.parent, false);
                dot.transform.localPosition = transform.localPosition;
                dot.transform.localScale = _baseScale * 0.16f;

                var renderer = dot.AddComponent<SpriteRenderer>();
                renderer.sprite = _baseRenderer.sprite;
                renderer.color = Boost(_baseColor, 1.15f);
                renderer.sortingLayerID = _baseRenderer.sortingLayerID;
                renderer.sortingOrder = _baseRenderer.sortingOrder + 2;

                StartCoroutine(AnimateBurst(dot.transform, renderer, direction * 0.34f, duration));
            }
        }

        private IEnumerator AnimateBurst(Transform dot, SpriteRenderer renderer, Vector3 offset, float duration)
        {
            Vector3 start = dot.localPosition;
            Vector3 end = start + offset;
            Color color = renderer.color;

            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                float p = Mathf.Clamp01(t / duration);
                dot.localPosition = Vector3.LerpUnclamped(start, end, EaseOutCubic(p));
                dot.localScale = Vector3.LerpUnclamped(_baseScale * 0.16f, _baseScale * 0.02f, p);
                color.a = 1f - p;
                renderer.color = color;
                yield return null;
            }

            Destroy(dot.gameObject);
        }

        private void SetRenderersAlpha(float alpha)
        {
            SetRendererAlpha(_baseRenderer, alpha);
            SetRendererAlpha(_protectorOverlay, alpha);
        }

        private static void SetRendererAlpha(SpriteRenderer renderer, float alpha)
        {
            if (renderer == null) return;
            Color color = renderer.color;
            color.a = alpha;
            renderer.color = color;
        }

        private static Color Boost(Color color, float amount)
        {
            return new Color(
                Mathf.Clamp01(color.r * amount),
                Mathf.Clamp01(color.g * amount),
                Mathf.Clamp01(color.b * amount),
                color.a);
        }

        private static float EaseOutCubic(float t)
        {
            t = Mathf.Clamp01(t);
            return 1f - Mathf.Pow(1f - t, 3f);
        }

        private static float EaseInCubic(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * t;
        }

        private static float EaseOutBack(float t)
        {
            t = Mathf.Clamp01(t);
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }
    }
}
