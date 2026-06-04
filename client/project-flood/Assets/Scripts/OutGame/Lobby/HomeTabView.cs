using System.Collections;
using System.Collections.Generic;
using Game.Services;
using ProjectFlood.Data.Generated;
using UnityEngine;
using UnityEngine.UI;

namespace Game.OutGame.Lobby
{
    public class HomeTabView : MonoBehaviour
    {
        [SerializeField] private ScrollRect   _scrollRect;
        [SerializeField] private RectTransform _contentRoot;
        [SerializeField] private GameObject   _stageNodePrefab;
        [SerializeField] private float        _nodeSpacingY = 140f;

        private readonly List<StageNodeView> _pool = new List<StageNodeView>();
        private Stage[]  _stages;
        private int      _currentStageId;

        private const string InGameScene = "InGame";

        private void OnEnable()
        {
            _stages         = StageDataService.Instance?.GetAll();
            _currentStageId = FindCurrentStage();
            BuildPool();
            RefreshVisible();
            RestoreScrollPosition();
        }

        private void OnDisable()
        {
            ScrollStateCache.HomeScrollPosition = _scrollRect != null
                ? _scrollRect.verticalNormalizedPosition : 1f;
        }

        private int FindCurrentStage()
        {
            if (_stages == null) return 1;
            var progress = PlayerProgressService.Instance;
            foreach (var s in _stages)
            {
                if (progress == null || !progress.IsStageUnlocked(s.stage_id))
                    break;
                if (progress.GetBestStars(s.stage_id) == 0)
                    return s.stage_id;
            }
            return _stages.Length > 0 ? _stages[^1].stage_id : 1;
        }

        private void BuildPool()
        {
            if (_stages == null) return;
            int needed = Mathf.Min(_stages.Length, Game.Core.GameConfig.StageNodePoolSize);
            while (_pool.Count < needed)
            {
                var go   = Instantiate(_stageNodePrefab, _contentRoot);
                var node = go.GetComponent<StageNodeView>();
                node.OnTapped += OnStageTapped;
                _pool.Add(node);
            }

            // layout: position all nodes
            float totalHeight = _stages.Length * _nodeSpacingY;
            _contentRoot.sizeDelta = new Vector2(_contentRoot.sizeDelta.x, totalHeight);

            for (int i = 0; i < _stages.Length && i < _pool.Count; i++)
            {
                var node = _pool[i];
                var rt   = node.GetComponent<RectTransform>();

                float y = -(i * _nodeSpacingY + _nodeSpacingY * 0.5f);
                int zigzag = i % 3;
                float x = zigzag == 0 ? 0f
                         : zigzag == 1 ? -Game.Core.GameConfig.StageNodeZigzagOffset
                         : Game.Core.GameConfig.StageNodeZigzagOffset;
                rt.anchoredPosition = new Vector2(x, y);
            }
        }

        private void RefreshVisible()
        {
            if (_stages == null) return;
            var progress = PlayerProgressService.Instance;

            for (int i = 0; i < _stages.Length && i < _pool.Count; i++)
            {
                var s       = _stages[i];
                int stars   = progress?.GetBestStars(s.stage_id) ?? 0;
                bool unlock = progress?.IsStageUnlocked(s.stage_id) ?? (s.stage_id == 1);
                bool cur    = s.stage_id == _currentStageId;
                _pool[i].Bind(s.stage_id, stars, unlock, cur);
                _pool[i].gameObject.SetActive(true);
            }
        }

        private void RestoreScrollPosition()
        {
            if (_scrollRect != null)
            {
                StartCoroutine(ApplyScrollNextFrame());
            }
        }

        private IEnumerator ApplyScrollNextFrame()
        {
            yield return null; // wait for layout rebuild
            _scrollRect.verticalNormalizedPosition = ScrollStateCache.HomeScrollPosition;
        }

        private void OnStageTapped(int stageId)
        {
            var stage = StageDataService.Instance?.GetStage(stageId);
            if (stage == null) return;

            var progress = PlayerProgressService.Instance;
            int stars    = progress?.GetBestStars(stageId) ?? 0;

            Game.Core.UIManager.Instance?.ShowPopup<StageInfoPopupView>(v => v.Init(
                stageId:   stageId,
                bestStars: stars,
                bestMoves: 0,
                onPlay:    () => EnterStage(stageId)));
        }

        private void EnterStage(int stageId)
        {
            ScrollStateCache.HomeScrollPosition = _scrollRect != null
                ? _scrollRect.verticalNormalizedPosition : 1f;
            ScrollStateCache.LastPlayedStageId = stageId;

            var transition = Game.Core.SceneTransition.Instance;
            if (transition != null)
                transition.SlideUpToScene(InGameScene);
            else
                UnityEngine.SceneManagement.SceneManager.LoadScene(InGameScene);
        }
    }
}
