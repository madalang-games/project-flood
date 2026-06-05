using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Game.Services;

namespace Game.OutGame.Lobby
{
    /// <summary>
    /// Stamina popup — shows current life count on heart icon, recharge countdown (HH:MM),
    /// and a Watch-Ad button that is dimmed/non-interactable when at MAX.
    /// Subscribes to StaminaApiService.OnStaminaUpdated for live refresh.
    /// </summary>
    public class StaminaPopupView : MonoBehaviour
    {
        [SerializeField] private TMP_Text    _countText;
        [SerializeField] private TMP_Text    _timerText;
        [SerializeField] private Button      _watchAdButton;
        [SerializeField] private CanvasGroup _watchAdButtonGroup; // dimmed at MAX
        [SerializeField] private Button      _closeButton;
        [SerializeField] private Button      _backdropButton;

        private void Awake()
        {
            _watchAdButton?.onClick.AddListener(OnWatchAd);
            _closeButton?.onClick.AddListener(Close);
            _backdropButton?.onClick.AddListener(Close);
        }

        private void OnEnable()
        {
            Refresh();
            var api = StaminaApiService.Instance;
            if (api != null) api.OnStaminaUpdated += Refresh;
        }

        private void OnDisable()
        {
            var api = StaminaApiService.Instance;
            if (api != null) api.OnStaminaUpdated -= Refresh;
        }

        private void Update() => RefreshTimer();

        private void Refresh()
        {
            var api = StaminaApiService.Instance;
            if (api == null) return;

            var latest = api.LatestStamina;
            if (latest == null) return;

            int estimated = api.GetEstimatedLife();

            if (_countText != null)
                _countText.text = latest.IsUnlimited ? "∞" : estimated.ToString();

            // Dim Watch Ad button when at MAX or unlimited
            bool isMax = !latest.IsUnlimited && estimated >= latest.Max;
            if (_watchAdButtonGroup != null)
            {
                _watchAdButtonGroup.alpha        = isMax ? 0.35f : 1f;
                _watchAdButtonGroup.interactable = !isMax;
                _watchAdButtonGroup.blocksRaycasts = !isMax;
            }

            RefreshTimer();
        }

        private void RefreshTimer()
        {
            var api = StaminaApiService.Instance;
            if (api == null || _timerText == null) return;

            var latest = api.LatestStamina;
            if (latest == null) return;

            if (latest.IsUnlimited)
            {
                var rem = api.GetSecondsOfUnlimitedRemaining();
                if (rem > 0)
                {
                    var ts = TimeSpan.FromSeconds(rem);
                    string formatted = ts.Hours > 0
                        ? $"{ts.Hours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}"
                        : $"{ts.Minutes:D2}:{ts.Seconds:D2}";
                    _timerText.text = string.Format(
                        LocalizationService.Instance.Get("popup.stamina.unlimited"), formatted);
                    return;
                }
            }

            int currentEstimate = api.GetEstimatedLife();
            if (latest.Max > 0 && currentEstimate >= latest.Max)
            {
                _timerText.text = LocalizationService.Instance.Get("stamina.max");
            }
            else
            {
                var sec = api.GetSecondsToNextRecharge();
                var ts  = TimeSpan.FromSeconds(sec);
                string countdown = $"{(int)ts.TotalMinutes:D2}:{ts.Seconds:D2}";
                _timerText.text = string.Format(
                    LocalizationService.Instance.Get("stamina.next_charge"), countdown);
            }
        }

        private void OnWatchAd()
        {
            var api = StaminaApiService.Instance;
            if (api == null) return;

            Game.Core.UIManager.Instance?.ShowLoading();
            api.ClaimAdLife("rewarded_video", "dummy_ad_token",
                onSuccess: _ =>
                {
                    Game.Core.UIManager.Instance?.HideLoading();
                    Game.Core.UIManager.Instance?.ShowToast("+1 Life!", Core.UI.ToastType.Success);
                    Refresh();
                },
                onError: err =>
                {
                    Game.Core.UIManager.Instance?.HideLoading();
                    Game.Core.UIManager.Instance?.ShowToast($"Ad error: {err}", Core.UI.ToastType.Warning);
                });
        }

        private void Close()
        {
            var appear = GetComponent<Core.UI.UIPanelAppear>();
            if (appear != null)
                appear.Disappear(() => Game.Core.UIManager.Instance?.CloseTopPopup());
            else
                Game.Core.UIManager.Instance?.CloseTopPopup();
        }
    }
}
