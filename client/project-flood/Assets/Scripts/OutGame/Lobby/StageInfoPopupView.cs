using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.OutGame.Lobby
{
    public class StageInfoPopupView : MonoBehaviour
    {
        [SerializeField] private TMP_Text   _stageTitle;
        [SerializeField] private TMP_Text   _bestRecord;
        [SerializeField] private Button     _playButton;
        [SerializeField] private Button     _backdropButton;
        [SerializeField] private GameObject[] _bestStarFills; // 3 stars

        private int    _stageId;
        private Action _onPlay;

        private void Awake()
        {
            _playButton.onClick.AddListener(OnPlay);
            if (_backdropButton != null) _backdropButton.onClick.AddListener(OnClose);
        }

        public void Init(int stageId, int bestStars, int bestMoves, Action onPlay)
        {
            _stageId   = stageId;
            _onPlay    = onPlay;

            if (_stageTitle  != null) _stageTitle.text  = $"Stage {stageId}";
            if (_bestRecord  != null) _bestRecord.text  = bestMoves > 0 ? $"Moves: {bestMoves}" : "-";

            for (int i = 0; i < _bestStarFills.Length; i++)
                if (_bestStarFills[i] != null) _bestStarFills[i].SetActive(i < bestStars);
        }

        private void OnPlay()
        {
            _onPlay?.Invoke();
            Close();
        }

        private void OnClose() => Close();

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
