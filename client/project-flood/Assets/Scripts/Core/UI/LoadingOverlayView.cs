using System.Collections;
using TMPro;
using UnityEngine;

namespace Game.Core.UI
{
    public class LoadingOverlayView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _messageText;

        private Coroutine _timeoutCoroutine;

        private void OnEnable()
        {
            if (_messageText != null) _messageText.gameObject.SetActive(false);
            _timeoutCoroutine = StartCoroutine(TimeoutWatch());
        }

        private void OnDisable()
        {
            if (_timeoutCoroutine != null) StopCoroutine(_timeoutCoroutine);
        }

        public void Show(string message = null)
        {
            gameObject.SetActive(true);
            if (_messageText != null)
            {
                _messageText.text = message ?? string.Empty;
                _messageText.gameObject.SetActive(!string.IsNullOrEmpty(message));
            }
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private IEnumerator TimeoutWatch()
        {
            yield return new WaitForSeconds(Game.Core.GameConfig.LoadingTimeoutSec);
            UIManager.Instance?.ShowNetworkError(null);
        }
    }
}
