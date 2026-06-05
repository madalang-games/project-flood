using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Game.Services;

namespace Game.OutGame.Lobby
{
    public class HeaderView : MonoBehaviour
    {
        [SerializeField] private Button   _avatarButton;
        [SerializeField] private TMP_Text _goldText;
        [SerializeField] private TMP_Text _staminaText;
        [SerializeField] private TMP_Text _staminaTimerText;

        private void Awake()
        {
            _avatarButton.onClick.AddListener(OnAvatarTapped);

            // Defensive check: auto-bind if null by searching children
            if (_staminaText == null)
            {
                var trans = transform.Find("StaminaPanel/Txt_Stamina") ?? transform.Find("Txt_Stamina") ?? transform.Find("Stamina/Text");
                if (trans != null) _staminaText = trans.GetComponent<TMP_Text>();
            }
            if (_staminaTimerText == null)
            {
                var trans = transform.Find("StaminaPanel/Txt_StaminaTimer") ?? transform.Find("Txt_StaminaTimer") ?? transform.Find("Stamina/Timer");
                if (trans != null) _staminaTimerText = trans.GetComponent<TMP_Text>();
            }
        }

        private void OnEnable()
        {
            UpdateStaminaUI();
        }

        private void Update()
        {
            UpdateStaminaUI();
        }

        public void SetGold(int amount)
        {
            if (_goldText != null) _goldText.text = amount.ToString("N0");
        }

        private void UpdateStaminaUI()
        {
            var staminaApi = StaminaApiService.Instance;
            if (staminaApi == null || staminaApi.LatestStamina == null)
            {
                if (_staminaText != null) _staminaText.text = "5/5";
                if (_staminaTimerText != null) _staminaTimerText.text = "";
                return;
            }

            var latest = staminaApi.LatestStamina;
            if (latest.IsUnlimited)
            {
                var remaining = staminaApi.GetSecondsOfUnlimitedRemaining();
                if (remaining > 0)
                {
                    if (_staminaText != null) _staminaText.text = "∞";
                    if (_staminaTimerText != null)
                    {
                        var ts = TimeSpan.FromSeconds(remaining);
                        _staminaTimerText.text = ts.Hours > 0 
                            ? $"{ts.Hours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}"
                            : $"{ts.Minutes:D2}:{ts.Seconds:D2}";
                    }
                    return;
                }
            }

            var estimatedLife = staminaApi.GetEstimatedLife();
            if (_staminaText != null)
                _staminaText.text = $"{estimatedLife}/{latest.Max}";

            if (estimatedLife >= latest.Max)
            {
                if (_staminaTimerText != null) _staminaTimerText.text = "MAX";
            }
            else
            {
                var secondsLeft = staminaApi.GetSecondsToNextRecharge();
                if (_staminaTimerText != null)
                {
                    var ts = TimeSpan.FromSeconds(secondsLeft);
                    _staminaTimerText.text = $"{ts.Minutes:D2}:{ts.Seconds:D2}";
                }
            }
        }

        private void OnAvatarTapped()
        {
            Game.Core.UIManager.Instance?.ShowPopup<Game.OutGame.Settings.AccountPopupView>();
        }
    }
}
