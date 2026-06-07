using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Game.InGame.View
{
    public class RowShiftOverlayView : MonoBehaviour
    {
        [SerializeField] private Image _pointerImage;
        [SerializeField] private RectTransform _dragLineRect;
        [SerializeField] private Sprite _pointerSprite;

        private Vector2 _startScreenPos;
        private Canvas _parentCanvas;
        private Coroutine _pointerAnimCoroutine;

        private void Awake()
        {
            _parentCanvas = GetComponentInParent<Canvas>();
            HideVisuals();
        }

        public void Show()
        {
            gameObject.SetActive(true);
            HideVisuals();
        }

        public void Hide()
        {
            gameObject.SetActive(false);
            HideVisuals();
        }

        public void StartDrag(Vector2 screenPos)
        {
            _startScreenPos = screenPos;
            
            if (_pointerImage != null)
            {
                _pointerImage.gameObject.SetActive(true);
                _pointerImage.sprite = _pointerSprite;
                UpdatePointerPosition(screenPos);

                if (_pointerAnimCoroutine != null) StopCoroutine(_pointerAnimCoroutine);
                _pointerAnimCoroutine = StartCoroutine(AnimatePointerScale());
            }

            if (_dragLineRect != null)
            {
                _dragLineRect.gameObject.SetActive(true);
                UpdateDragLine(screenPos);
            }
        }

        public void Dragging(Vector2 screenPos)
        {
            UpdatePointerPosition(screenPos);
            UpdateDragLine(screenPos);
        }

        public void EndDrag()
        {
            HideVisuals();
        }

        private void HideVisuals()
        {
            if (_pointerImage != null)
            {
                _pointerImage.gameObject.SetActive(false);
                if (_pointerAnimCoroutine != null)
                {
                    StopCoroutine(_pointerAnimCoroutine);
                    _pointerAnimCoroutine = null;
                }
            }
            if (_dragLineRect != null) _dragLineRect.gameObject.SetActive(false);
        }

        private IEnumerator AnimatePointerScale()
        {
            while (true)
            {
                float t = Time.time * 5f;
                float scaleFactor = 1.0f + Mathf.Sin(t) * 0.12f;
                _pointerImage.rectTransform.localScale = Vector3.one * scaleFactor;
                yield return null;
            }
        }

        private void UpdatePointerPosition(Vector2 screenPos)
        {
            if (_pointerImage == null) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                (RectTransform)transform,
                screenPos,
                _parentCanvas.worldCamera,
                out Vector2 localPos
            );
            _pointerImage.rectTransform.anchoredPosition = localPos;
        }

        private void UpdateDragLine(Vector2 screenPos)
        {
            if (_dragLineRect == null) return;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                (RectTransform)transform,
                _startScreenPos,
                _parentCanvas.worldCamera,
                out Vector2 localStart
            );

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                (RectTransform)transform,
                screenPos,
                _parentCanvas.worldCamera,
                out Vector2 localCurrent
            );

            float deltaX = localCurrent.x - localStart.x;
            _dragLineRect.sizeDelta = new Vector2(Mathf.Abs(deltaX), _dragLineRect.sizeDelta.y);
            _dragLineRect.anchoredPosition = new Vector2(localStart.x + deltaX / 2f, localStart.y);
        }
    }
}
