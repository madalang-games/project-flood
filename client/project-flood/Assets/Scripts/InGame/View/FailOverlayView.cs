using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Game.Services;
using Game.Core;
using Game.Core.UI;

namespace Game.InGame.View
{
    public class FailOverlayView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _continueLabel;
        [SerializeField] private TMP_Text _costText;
        [SerializeField] private TMP_Text _ownedGoldText;
        [SerializeField] private Button   _continueButton;
        [SerializeField] private Button   _forfeitButton;
        [SerializeField] private Button   _watchAdButton;
        [SerializeField] private TMP_Text _reviveLimitText;

        private Action _onContinue;
        private Action _onForfeit;
        private Action<int> _onReviveSuccess;

        private float    _cooldownTimer = 0f;
        private TMP_Text _watchAdButtonText;
        private string   _originalWatchAdButtonText = "Watch Ad";

        private void Awake()
        {
            _continueButton.onClick.AddListener(OnContinue);
            _forfeitButton.onClick.AddListener(OnForfeit);
            if (_watchAdButton != null)
            {
                _watchAdButton.onClick.AddListener(OnWatchAd);
                _watchAdButtonText = _watchAdButton.GetComponentInChildren<TMP_Text>();
                if (_watchAdButtonText != null)
                    _originalWatchAdButtonText = _watchAdButtonText.text;
            }
        }

        public void Init(int continueCost, int currentGold, Action onContinue, Action onForfeit, Action<int> onReviveSuccess = null)
        {
            _onContinue = onContinue;
            _onForfeit  = onForfeit;
            _onReviveSuccess = onReviveSuccess;

            if (_continueLabel  != null) 
                _continueLabel.text = string.Format(LocalizationService.Instance.Get("popup.fail.continue_turns"), GameConfig.ContinueExtraTurns);
            if (_costText       != null) _costText.text      = $"{continueCost}";
            if (_ownedGoldText  != null) _ownedGoldText.text = $"{currentGold}";

            bool canAfford = currentGold >= continueCost;
            var animator   = _continueButton.GetComponent<UIButtonAnimator>();
            if (animator != null) animator.SetInteractable(canAfford);
            else _continueButton.interactable = canAfford;

            RefreshAdButton();
        }

        private void Update()
        {
            if (_cooldownTimer > 0f)
            {
                _cooldownTimer -= Time.deltaTime;
                if (_cooldownTimer <= 0f)
                {
                    _cooldownTimer = 0f;
                    if (_watchAdButtonText != null) _watchAdButtonText.text = _originalWatchAdButtonText;
                    RefreshAdButton();
                }
                else
                {
                    if (_watchAdButtonText != null)
                        _watchAdButtonText.text = $"{_originalWatchAdButtonText} ({Mathf.CeilToInt(_cooldownTimer)}s)";
                }
            }
        }

        private void RefreshAdButton()
        {
            if (StageApiService.Instance == null || StageApiService.Instance.CurrentAttempt == null)
            {
                if (_watchAdButton != null) _watchAdButton.gameObject.SetActive(false);
                if (_reviveLimitText != null) _reviveLimitText.gameObject.SetActive(false);
                return;
            }

            var attempt = StageApiService.Instance.CurrentAttempt;
            int remaining = attempt.RemainingRevives;
            
            if (_reviveLimitText != null)
                _reviveLimitText.text = $"Remaining Revives: {remaining}";

            if (_watchAdButton != null)
            {
                bool showButton = remaining > 0;
                _watchAdButton.gameObject.SetActive(showButton);
                if (showButton)
                {
                    _watchAdButton.interactable = _cooldownTimer <= 0f;
                }
            }
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

        private void OnWatchAd()
        {
            if (AdMobService.Instance == null || StageApiService.Instance == null || StageApiService.Instance.CurrentAttempt == null) return;

            var attempt = StageApiService.Instance.CurrentAttempt;
            int stageId = attempt.StageId;
            string attemptId = attempt.AttemptId;

            _cooldownTimer = 30f;
            RefreshAdButton();

            UIManager.Instance?.ShowLoading();
            
            AdMobService.Instance.WatchRewardedAd("STAGE_REVIVE", result =>
            {
                if (result.HasValue && result.Value.Earned)
                {
                    StageApiService.Instance.ReviveAd(stageId, attemptId, "rewarded_video", result.Value.AdToken,
                        onSuccess: response =>
                        {
                            UIManager.Instance?.HideLoading();
                            UIManager.Instance?.ShowToast("Revived! Turns granted.", ToastType.Success);
                            _onReviveSuccess?.Invoke(response.TurnsGranted);
                            Destroy(gameObject);
                        },
                        onError: err =>
                        {
                            UIManager.Instance?.HideLoading();
                            UIManager.Instance?.ShowToast($"Revive failed: {err}", ToastType.Warning);
                            
                            _cooldownTimer = 0f;
                            if (_watchAdButtonText != null) _watchAdButtonText.text = _originalWatchAdButtonText;
                            RefreshAdButton();
                        });
                }
                else
                {
                    UIManager.Instance?.HideLoading();
                    UIManager.Instance?.ShowToast("Ad failed or was cancelled.", ToastType.Warning);

                    _cooldownTimer = 0f;
                    if (_watchAdButtonText != null) _watchAdButtonText.text = _originalWatchAdButtonText;
                    RefreshAdButton();
                }
            });
        }
    }
}
