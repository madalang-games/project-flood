using System.Collections;
using Game.Core;
using Game.Core.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.InGame.View
{
    public class HUDView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _turnsText;
        [SerializeField] private TMP_Text _remainingText;
        [SerializeField] private Button   _pauseButton;
        [SerializeField] private Image[]  _starImages;  // 3 elements: star1, star2, star3
        [SerializeField] private Sprite   _starFilled;
        [SerializeField] private Sprite   _starEmpty;
        [SerializeField] private Image    _turnsBorder;
        [SerializeField] private Image      _stageInfoBg;
        [SerializeField] private TMP_Text   _stageText;
        [SerializeField] private GameObject _skullBadge;

        [Header("Border Colors")]
        [SerializeField] private Color _safeColor       = new Color(0.24f, 1f,   0.47f, 1f);
        [SerializeField] private Color _cautionColor    = new Color(1f,   0.85f, 0f,    1f);
        [SerializeField] private Color _dangerColor     = new Color(1f,   0.13f, 0.27f, 1f);
        [SerializeField] private Color _dangerPulseColor = new Color(1f,  0.47f, 0.60f, 1f);
        [SerializeField] private float _pulseDuration   = 0.8f;

        public event System.Action OnPausePressed;

        private float          _star1Ratio;
        private float          _star2Ratio;
        private int            _initialValidCells;
        private UINumberChange _turnsAnim;
        private UINumberChange _remainingAnim;
        private Coroutine      _pulseCoroutine;

        private void Awake()
        {
            _pauseButton.onClick.AddListener(() => OnPausePressed?.Invoke());
            _turnsAnim    = _turnsText?.GetComponent<UINumberChange>();
            _remainingAnim = _remainingText?.GetComponent<UINumberChange>();
        }

        private void OnDestroy()
        {
            if (_pulseCoroutine != null) StopCoroutine(_pulseCoroutine);
        }

        public void Init(int totalTurns, int initialValidCells, float star1Ratio, float star2Ratio, int difficulty = 0, int stageNumber = 0)
        {
            _initialValidCells = initialValidCells;
            _star1Ratio = star1Ratio;
            _star2Ratio = star2Ratio;
            UpdateTurns(totalTurns);
            UpdateRemainingCells(initialValidCells);

            if (_stageInfoBg != null)
                _stageInfoBg.color = DifficultyStyle.Get(difficulty, new Color(0.302f, 0.137f, 0.365f, 1f));
            if (_stageText != null)
                _stageText.text = stageNumber > 0 ? stageNumber.ToString() : "";
            if (_skullBadge != null)
                _skullBadge.SetActive(difficulty == 2);
        }

        public void UpdateTurns(int remaining)
        {
            if (_turnsAnim != null) _turnsAnim.Set(remaining);
            else if (_turnsText != null) _turnsText.text = remaining.ToString();
            RefreshBorderColor(remaining);
        }

        private void RefreshBorderColor(int remaining)
        {
            if (_turnsBorder == null) return;

            if (remaining <= 3)
            {
                if (_pulseCoroutine == null)
                    _pulseCoroutine = StartCoroutine(PulseBorder());
                return;
            }

            if (_pulseCoroutine != null)
            {
                StopCoroutine(_pulseCoroutine);
                _pulseCoroutine = null;
            }

            Color borderColor;
            if (remaining > 10)
            {
                borderColor = _safeColor;
            }
            else if (remaining > 5)
            {
                float t = (remaining - 5f) / 5f; // 0 at 5turns, 1 at 10turns
                borderColor = Color.Lerp(_cautionColor, _safeColor, t);
            }
            else // 3 < remaining <= 5
            {
                float t = (remaining - 3f) / 2f; // 0 at 3turns, 1 at 5turns
                borderColor = Color.Lerp(_dangerColor, _cautionColor, t);
            }

            _turnsBorder.color = borderColor;
        }

        private IEnumerator PulseBorder()
        {
            while (true)
            {
                float t = Mathf.PingPong(Time.time / _pulseDuration, 1f);
                _turnsBorder.color = Color.Lerp(_dangerColor, _dangerPulseColor, t);
                yield return null;
            }
        }

        public void UpdateRemainingCells(int remaining)
        {
            if (_remainingAnim != null) _remainingAnim.Set(remaining);
            else if (_remainingText != null) _remainingText.text = remaining.ToString();
            RefreshStars(remaining);
        }

        private void RefreshStars(int remaining)
        {
            if (_starImages == null || _starImages.Length < 3 || _starFilled == null || _starEmpty == null) return;

            float ratio = _initialValidCells > 0
                ? (_initialValidCells - remaining) / (float)_initialValidCells
                : 0f;

            int filled = 0;
            if (remaining == 0)            filled = 3;
            else if (ratio >= _star2Ratio) filled = 2;
            else if (ratio >= _star1Ratio) filled = 1;

            for (int i = 0; i < 3; i++)
                if (_starImages[i] != null)
                    _starImages[i].sprite = i < filled ? _starFilled : _starEmpty;
        }
    }
}
