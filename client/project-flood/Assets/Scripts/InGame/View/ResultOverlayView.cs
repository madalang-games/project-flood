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

            if (_titleText != null) _titleText.text = fail ? "Stage Failed" : $"Stage {stageId} Clear!";

            if (_ratioText != null)
                _ratioText.text = $"Cleared: {clearanceRatio * 100f:F0}%";

            if (_turnsText != null)
                _turnsText.text = $"Turns used: {turnsUsed}/{totalTurns}";
            if (_rankText != null)
                _rankText.text = "";

            if (_goldRow != null) _goldRow.SetActive(!fail);
            if (_goldText != null && !fail)
                _goldText.text = $"+{goldEarned}";

            if (_nextButton != null) _nextButton.gameObject.SetActive(!nextLocked && !fail);

            // Hide all stars initially; UIStarPop animates them
            for (int i = 0; i < _starObjects.Length; i++)
            {
                if (_starObjects[i] == null) continue;
                var rt = _starObjects[i].GetComponent<RectTransform>();
                if (rt != null && i < stars)
                    rt.localScale = Vector3.zero;
                _starObjects[i].SetActive(true);
            }

            StartCoroutine(PlayStarSequence(stars));
        }

        public void SetServerRank(int? stageRank, bool isNewBest)
        {
            if (_rankText == null) return;
            _rankText.text = stageRank.HasValue
                ? $"Stage Rank: #{stageRank.Value}" + (isNewBest ? "  New Best" : "")
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
