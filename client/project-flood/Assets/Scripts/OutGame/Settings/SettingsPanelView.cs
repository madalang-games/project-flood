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
        [SerializeField] private Slider   _bgmSlider;
        [SerializeField] private Slider   _sfxSlider;
        [SerializeField] private Button   _accountButton;
        [SerializeField] private Button   _backdropButton;
        [SerializeField] private TMP_Text _versionText;

        private const string KeyBGM         = "setting_bgm";
        private const string KeySFX         = "setting_sfx";
        private const string KeyScreenShake = "setting_screen_shake";

        private void Awake()
        {
            if (Game.Services.SoundManager.Instance != null)
            {
                var snd = Game.Services.SoundManager.Instance;
                _bgmToggle.isOn = !snd.BGMMute;
                _sfxToggle.isOn = !snd.SFXMute;

                if (_bgmSlider != null)
                {
                    _bgmSlider.value = snd.BGMVolume;
                    _bgmSlider.onValueChanged.AddListener(v => snd.BGMVolume = v);
                }
                if (_sfxSlider != null)
                {
                    _sfxSlider.value = snd.SFXVolume;
                    _sfxSlider.onValueChanged.AddListener(v => snd.SFXVolume = v);
                }

                _bgmToggle.onValueChanged.AddListener(v => snd.BGMMute = !v);
                _sfxToggle.onValueChanged.AddListener(v => snd.SFXMute = !v);
            }
            else
            {
                _bgmToggle.isOn         = PlayerPrefs.GetInt(KeyBGM,         1) == 1;
                _sfxToggle.isOn         = PlayerPrefs.GetInt(KeySFX,         1) == 1;
                _bgmToggle.onValueChanged.AddListener(v         => { PlayerPrefs.SetInt(KeyBGM,         v ? 1 : 0); });
                _sfxToggle.onValueChanged.AddListener(v         => { PlayerPrefs.SetInt(KeySFX,         v ? 1 : 0); });
            }

            _screenShakeToggle.isOn = PlayerPrefs.GetInt(KeyScreenShake, 1) == 1;
            _screenShakeToggle.onValueChanged.AddListener(v => { PlayerPrefs.SetInt(KeyScreenShake, v ? 1 : 0); });

            _accountButton.onClick.AddListener(OnAccountTapped);
            if (_backdropButton != null) _backdropButton.onClick.AddListener(Close);

            if (_versionText != null) _versionText.text = $"v{Application.version}";
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
