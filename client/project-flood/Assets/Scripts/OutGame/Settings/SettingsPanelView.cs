using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.OutGame.Settings
{
    public class SettingsPanelView : MonoBehaviour
    {
        [SerializeField] private Toggle   _bgmToggle;
        [SerializeField] private Toggle   _sfxToggle;
        [SerializeField] private Toggle   _screenShakeToggle;
        [SerializeField] private Button   _accountButton;
        [SerializeField] private Button   _backdropButton;
        [SerializeField] private TMP_Text _versionText;

        private const string KeyBGM         = "setting_bgm";
        private const string KeySFX         = "setting_sfx";
        private const string KeyScreenShake = "setting_screen_shake";

        private void Awake()
        {
            _bgmToggle.isOn         = PlayerPrefs.GetInt(KeyBGM,         1) == 1;
            _sfxToggle.isOn         = PlayerPrefs.GetInt(KeySFX,         1) == 1;
            _screenShakeToggle.isOn = PlayerPrefs.GetInt(KeyScreenShake, 1) == 1;

            _bgmToggle.onValueChanged.AddListener(v         => { PlayerPrefs.SetInt(KeyBGM,         v ? 1 : 0); ApplyBGM(v); });
            _sfxToggle.onValueChanged.AddListener(v         => { PlayerPrefs.SetInt(KeySFX,         v ? 1 : 0); ApplySFX(v); });
            _screenShakeToggle.onValueChanged.AddListener(v => { PlayerPrefs.SetInt(KeyScreenShake, v ? 1 : 0); });

            _accountButton.onClick.AddListener(OnAccountTapped);
            if (_backdropButton != null) _backdropButton.onClick.AddListener(Close);

            if (_versionText != null) _versionText.text = $"v{Application.version}";
        }

        private static void ApplyBGM(bool on)
        {
            AudioListener.volume = (on || PlayerPrefs.GetInt("setting_sfx", 1) == 1) ? 1f : 0f;
        }

        private static void ApplySFX(bool on)
        {
            // Phase 2: SFX AudioMixer control
        }

        private void OnAccountTapped()
        {
            Game.Core.UIManager.Instance?.ShowPopup<AccountPopupView>();
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
