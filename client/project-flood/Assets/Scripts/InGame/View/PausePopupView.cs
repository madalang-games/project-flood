using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.InGame.View
{
    public class PausePopupView : MonoBehaviour
    {
        [SerializeField] private Button _resumeButton;
        [SerializeField] private Button _restartButton;
        [SerializeField] private Button _settingsButton;
        [SerializeField] private Button _stageSelectButton;

        public event Action OnResume;
        public event Action OnRestart;
        public event Action OnSettings;
        public event Action OnStageSelect;

        private void Awake()
        {
            _resumeButton.onClick.AddListener(      () => { OnResume?.Invoke();      Close(); });
            _restartButton.onClick.AddListener(     () => { OnRestart?.Invoke();     Close(); });
            _settingsButton.onClick.AddListener(    () => OnSettings?.Invoke());
            _stageSelectButton.onClick.AddListener( () => { OnStageSelect?.Invoke(); Close(); });
        }

        private void Close()
        {
            Game.Core.UIManager.Instance?.CloseOverlay();
        }
    }
}
