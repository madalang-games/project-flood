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

        public event System.Action OnPausePressed;

        private void Awake()
        {
            _pauseButton.onClick.AddListener(() => OnPausePressed?.Invoke());
        }

        public void Init(int totalTurns, int initialValidCells)
        {
            UpdateTurns(totalTurns);
            UpdateRemainingCells(initialValidCells);
        }

        public void UpdateTurns(int remaining)
        {
            if (_turnsText != null) _turnsText.text = remaining.ToString();
        }

        public void UpdateRemainingCells(int remaining)
        {
            if (_remainingText != null) _remainingText.text = remaining.ToString();
        }
    }
}
