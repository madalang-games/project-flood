using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Game.Core
{
    public class SceneTransition : MonoBehaviour
    {
        public static SceneTransition Instance { get; private set; }

        [SerializeField] private Image _fadeImage;
        [SerializeField] private RectTransform _slidePanel;

        private const float FadeDuration  = 0.25f;
        private const float SlideDuration = 0.25f;

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            if (_fadeImage != null) SetAlpha(_fadeImage, 0f);
        }

        public void FadeToScene(string sceneName, Action onMidpoint = null)
            => StartCoroutine(FadeSequence(sceneName, onMidpoint));

        public void SlideUpToScene(string sceneName, Action onMidpoint = null)
            => StartCoroutine(SlideSequence(sceneName, Vector2.up,   onMidpoint));

        public void SlideDownToScene(string sceneName, Action onMidpoint = null)
            => StartCoroutine(SlideSequence(sceneName, Vector2.down, onMidpoint));

        private IEnumerator FadeSequence(string sceneName, Action onMidpoint)
        {
            yield return FadePanel(_fadeImage, 0f, 1f, FadeDuration);
            onMidpoint?.Invoke();
            SceneManager.LoadScene(sceneName);
            yield return FadePanel(_fadeImage, 1f, 0f, FadeDuration);
        }

        private IEnumerator SlideSequence(string sceneName, Vector2 direction, Action onMidpoint)
        {
            if (_slidePanel != null)
                yield return SlidePanel(_slidePanel, direction,  SlideDuration);

            onMidpoint?.Invoke();
            SceneManager.LoadScene(sceneName);

            if (_slidePanel != null)
            {
                _slidePanel.anchoredPosition = -direction * GetPanelSize(_slidePanel);
                yield return SlidePanel(_slidePanel, Vector2.zero, SlideDuration);
            }
        }

        private static IEnumerator FadePanel(Graphic graphic, float from, float to, float duration)
        {
            if (graphic == null) yield break;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                SetAlpha(graphic, Mathf.Lerp(from, to, t));
                yield return null;
            }
            SetAlpha(graphic, to);
        }

        private static IEnumerator SlidePanel(RectTransform rt, Vector2 targetOffset, float duration)
        {
            var start = rt.anchoredPosition;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Game.Core.UI.UIEasing.EaseInOut(Mathf.Clamp01(elapsed / duration));
                rt.anchoredPosition = Vector2.Lerp(start, targetOffset, t);
                yield return null;
            }
            rt.anchoredPosition = targetOffset;
        }

        private static void SetAlpha(Graphic graphic, float alpha)
        {
            var c = graphic.color;
            c.a = alpha;
            graphic.color = c;
        }

        private static float GetPanelSize(RectTransform rt) => Mathf.Max(rt.rect.width, rt.rect.height, Screen.height);
    }
}
