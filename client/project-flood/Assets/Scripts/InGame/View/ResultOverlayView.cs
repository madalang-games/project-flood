using System;
using System.Collections;
using Game.InGame.Board;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.InGame.View
{
    public class ResultOverlayView : MonoBehaviour
    {
        [SerializeField] private TMP_Text    _titleText;
        [SerializeField] private TMP_Text    _ratioText;
        [SerializeField] private TMP_Text    _turnsText;
        [SerializeField] private TMP_Text    _rankText;
        [SerializeField] private TMP_Text    _goldText;
        [SerializeField] private GameObject[] _starObjects; // 3 stars
        [SerializeField] private Button      _retryButton;
        [SerializeField] private Button      _nextButton;
        [SerializeField] private Button      _mapButton;
        [SerializeField] private GameObject  _goldRow;

        public event Action OnRetry;
        public event Action OnNext;
        public event Action OnMap;

        private const string LobbyScene = "Lobby";

        private void Awake()
        {
            _retryButton.onClick.AddListener(() => OnRetry?.Invoke());
            _nextButton.onClick.AddListener(() =>  OnNext?.Invoke());
            _mapButton.onClick.AddListener(() =>   OnMap?.Invoke());
        }

        public void Init(StarResult result, int stageId, int turnsUsed, int totalTurns,
                         float clearanceRatio, int goldEarned, bool nextLocked)
        {
            bool fail = result == StarResult.Fail;
            int  stars = (int)result;

            if (_titleText != null) 
                _titleText.text = fail ? Game.Services.LocalizationService.Instance.Get("popup.result.failed") : string.Format(Game.Services.LocalizationService.Instance.Get("popup.result.clear_title"), stageId);

            if (_ratioText != null)
                _ratioText.text = string.Format(Game.Services.LocalizationService.Instance.Get("popup.result.ratio"), (clearanceRatio * 100f).ToString("F0"));

            if (_turnsText != null)
                _turnsText.text = string.Format(Game.Services.LocalizationService.Instance.Get("popup.result.turns"), turnsUsed, totalTurns);
            if (_rankText != null)
                _rankText.text = "";

            bool showGold = !fail && goldEarned > 0;
            if (_goldRow != null) _goldRow.SetActive(showGold);
            if (_goldText != null && showGold)
                _goldText.text = $"+{goldEarned}";

            if (_nextButton != null) _nextButton.gameObject.SetActive(!nextLocked && !fail);

            StartCoroutine(PlayStarSequence(stars));
        }

        public void SetServerRank(int? stageRank, bool isNewBest)
        {
            if (_rankText == null) return;
            _rankText.text = stageRank.HasValue
                ? string.Format(Game.Services.LocalizationService.Instance.Get("popup.result.rank"), stageRank.Value) + (isNewBest ? Game.Services.LocalizationService.Instance.Get("popup.result.new_best") : "")
                : "";
        }

        private IEnumerator PlayStarSequence(int filledCount)
        {
            yield return new WaitForSeconds(0.3f);
            var starPop = GetComponent<Core.UI.UIStarPop>();
            if (starPop == null) starPop = gameObject.AddComponent<Core.UI.UIStarPop>();
            yield return starPop.PlayStarSequence(_starObjects, filledCount);
        }
    }
}
