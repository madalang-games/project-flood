using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.InGame.View
{
    public class FailOverlayView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _continueLabel;
        [SerializeField] private TMP_Text _costText;
        [SerializeField] private TMP_Text _ownedGoldText;
        [SerializeField] private Button   _continueButton;
        [SerializeField] private Button   _forfeitButton;

        private Action _onContinue;
        private Action _onForfeit;

        private void Awake()
        {
            _continueButton.onClick.AddListener(OnContinue);
            _forfeitButton.onClick.AddListener(OnForfeit);
        }

        public void Init(int continueCost, int currentGold, Action onContinue, Action onForfeit)
        {
            _onContinue = onContinue;
            _onForfeit  = onForfeit;

            if (_continueLabel  != null) 
                _continueLabel.text = string.Format(Game.Services.LocalizationService.Instance.Get("popup.fail.continue_turns"), Game.Core.GameConfig.ContinueExtraTurns);
            if (_costText       != null) _costText.text      = $"{continueCost}";
            if (_ownedGoldText  != null) _ownedGoldText.text = $"{currentGold}";

            bool canAfford = currentGold >= continueCost;
            var animator   = _continueButton.GetComponent<Core.UI.UIButtonAnimator>();
            if (animator != null) animator.SetInteractable(canAfford);
            else _continueButton.interactable = canAfford;
        }

        private void OnContinue()
        {
            _onContinue?.Invoke();
            Destroy(gameObject);
        }

        private void OnForfeit()
        {
            _onForfeit?.Invoke();
            Destroy(gameObject);
        }
    }
}
