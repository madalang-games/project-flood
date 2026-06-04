using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.InGame.View
{
    public class HUDView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _turnsText;
        [SerializeField] private Image    _ratioFill;
        [SerializeField] private Button   _pauseButton;

        // Anchored position markers (x = normalized ratio value)
        [SerializeField] private RectTransform _star1Marker;
        [SerializeField] private RectTransform _star2Marker;

        private static readonly Color SuccessColor = new Color(0.24f, 0.75f, 0.43f);
        private static readonly Color DangerColor  = new Color(0.91f, 0.25f, 0.25f);

        public event System.Action OnPausePressed;

        private float _star1Ratio;
        private float _star2Ratio;

        private void Awake()
        {
            _pauseButton.onClick.AddListener(() => OnPausePressed?.Invoke());
        }

        public void Init(int totalTurns, float star1Ratio, float star2Ratio)
        {
            _star1Ratio = star1Ratio;
            _star2Ratio = star2Ratio;
            UpdateTurns(totalTurns);
            UpdateRatio(0f);

            if (_star1Marker != null)
            {
                var rect = _ratioFill.rectTransform.rect;
                _star1Marker.anchoredPosition = new Vector2(rect.width * star1Ratio, _star1Marker.anchoredPosition.y);
            }
            if (_star2Marker != null)
            {
                var rect = _ratioFill.rectTransform.rect;
                _star2Marker.anchoredPosition = new Vector2(rect.width * star2Ratio, _star2Marker.anchoredPosition.y);
            }
        }

        public void UpdateTurns(int remaining)
        {
            if (_turnsText != null) _turnsText.text = remaining.ToString();
        }

        public void UpdateRatio(float ratio)
        {
            if (_ratioFill == null) return;
            _ratioFill.fillAmount = Mathf.Clamp01(ratio);
            _ratioFill.color      = ratio >= _star1Ratio ? SuccessColor : DangerColor;
        }
    }
}
