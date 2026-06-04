using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Core.UI
{
    public enum ToastType { Warning, Success, Error }

    public class ToastView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _messageText;
        [SerializeField] private Image    _iconImage;
        [SerializeField] private Sprite   _warningIcon;
        [SerializeField] private Sprite   _successIcon;
        [SerializeField] private Sprite   _errorIcon;

        private Coroutine _dismissCoroutine;
        private RectTransform _rt;
        private CanvasGroup   _cg;

        private const float DisplayDuration = 2.5f;
        private const float AppearDuration  = 0.15f;
        private const float DismissDuration = 0.2f;
        private const float SlideDistance   = 40f;

        private void Awake()
        {
            _rt = GetComponent<RectTransform>();
            _cg = GetComponent<CanvasGroup>();
            if (_cg == null) _cg = gameObject.AddComponent<CanvasGroup>();
        }

        public void Show(string message, ToastType type)
        {
            if (_dismissCoroutine != null) StopCoroutine(_dismissCoroutine);
            gameObject.SetActive(true);
            _messageText.text = message;
            if (_iconImage != null)
            {
                _iconImage.sprite = type switch
                {
                    ToastType.Success => _successIcon,
                    ToastType.Error   => _errorIcon,
                    _                 => _warningIcon,
                };
            }
            _dismissCoroutine = StartCoroutine(ShowSequence());
        }

        public void Hide()
        {
            if (_dismissCoroutine != null) StopCoroutine(_dismissCoroutine);
            gameObject.SetActive(false);
        }

        private IEnumerator ShowSequence()
        {
            yield return Appear();
            yield return new WaitForSeconds(DisplayDuration);
            yield return Dismiss();
            gameObject.SetActive(false);
        }

        private IEnumerator Appear()
        {
            var baseY = _rt.anchoredPosition.y;
            float elapsed = 0f;
            while (elapsed < AppearDuration)
            {
                elapsed += Time.deltaTime;
                float t = UIEasing.EaseOut(Mathf.Clamp01(elapsed / AppearDuration));
                _rt.anchoredPosition = new Vector2(_rt.anchoredPosition.x, Mathf.Lerp(baseY - SlideDistance, baseY, t));
                _cg.alpha = t;
                yield return null;
            }
            _cg.alpha = 1f;
        }

        private IEnumerator Dismiss()
        {
            float elapsed = 0f;
            while (elapsed < DismissDuration)
            {
                elapsed += Time.deltaTime;
                _cg.alpha = 1f - Mathf.Clamp01(elapsed / DismissDuration);
                yield return null;
            }
            _cg.alpha = 0f;
        }
    }
}
