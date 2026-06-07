using System.Collections;
using System.Collections.Generic;
using Game.Core.UI;
using Game.Services;
using ProjectFlood.Data.Generated;
using UnityEngine;
using UnityEngine.UI;

namespace Game.OutGame.Lobby
{
    public class HomeTabView : MonoBehaviour
    {
        [SerializeField] private ScrollRect    _scrollRect;
        [SerializeField] private RectTransform _contentRoot;
        [SerializeField] private GameObject    _stageNodePrefab;
        [SerializeField] private float         _nodeSpacingY      = 200f;
        [SerializeField] private float         _connectorTurnGap = 375f; // Y gap: row-end→connector and connector→next-row
        [SerializeField] private Color         _pathColor    = new Color(0.5f, 0.7f, 1f, 0.5f);
        [SerializeField] private float         _pathWidth    = 12f;

        private readonly List<StageNodeView> _pool = new List<StageNodeView>();
        private UILineStrip _pathStrip;
        private Stage[] _stages;
        private int     _currentStageId;

        private readonly List<StageNodeView> _chestNodes = new List<StageNodeView>();
        private readonly Dictionary<string, bool> _chestClaimed = new Dictionary<string, bool>
        {
            { "chapter1_chest", false },
            { "chapter2_chest", false },
            { "chapter3_chest", false },
        };

        private const string InGameScene = "InGame";

        private void Awake()
        {
            // ScrollRect needs a raycast target on the viewport to receive drag input
            // over empty space. Add a transparent Image if one doesn't exist.
            var viewport = _scrollRect != null ? _scrollRect.viewport : null;
            if (viewport != null && viewport.GetComponent<Image>() == null)
            {
                var img = viewport.gameObject.AddComponent<Image>();
                img.color          = Color.clear;
                img.raycastTarget  = true;
            }
        }

        private void OnEnable()
        {
            _stages         = StageDataService.Instance?.GetAll();
            _currentStageId = FindCurrentStage();
            BuildPool();
            RefreshVisible();
            RestoreScrollPosition();

            if (RewardsApiService.Instance != null)
            {
                RewardsApiService.Instance.FetchRewardSources(response =>
                {
                    _chestClaimed["chapter1_chest"] = true;
                    _chestClaimed["chapter2_chest"] = true;
                    _chestClaimed["chapter3_chest"] = true;

                    foreach (var src in response.Sources)
                    {
                        if (_chestClaimed.ContainsKey(src.SourceId))
                        {
                            _chestClaimed[src.SourceId] = !src.Claimable;
                        }
                    }
                    RefreshVisible();
                }, err => Debug.LogWarning($"[HomeTabView] failed to fetch reward sources: {err}"));
            }
        }

        private void OnDisable()
        {
            ScrollStateCache.HomeScrollPosition = _scrollRect != null
                ? _scrollRect.verticalNormalizedPosition : 0f;
        }

        private int FindCurrentStage()
        {
            if (_stages == null) return 1;
            var progress = PlayerProgressService.Instance;
            foreach (var s in _stages)
            {
                if (progress == null || !progress.IsStageUnlocked(s.stage_id)) break;
                if (progress.GetBestStars(s.stage_id) == 0) return s.stage_id;
            }
            return _stages.Length > 0 ? _stages[^1].stage_id : 1;
        }

        private void BuildPool()
        {
            if (_stages == null) return;

            if (_pathStrip != null) { Destroy(_pathStrip.gameObject); _pathStrip = null; }

            // Clean up editor dummy placeholder nodes so they don't overlap at runtime
            for (int i = _contentRoot.childCount - 1; i >= 0; i--)
            {
                var child = _contentRoot.GetChild(i);
                if (child.gameObject.name.StartsWith("StageNode_"))
                {
                    Destroy(child.gameObject);
                }
            }

            // Deactivate all pool nodes — prevents stale 0-position nodes on re-enable
            foreach (var n in _pool) n.gameObject.SetActive(false);

            int needed = Mathf.Min(_stages.Length, Game.Core.GameConfig.StageNodePoolSize);
            while (_pool.Count < needed)
            {
                var go   = Instantiate(_stageNodePrefab, _contentRoot);
                var node = go.GetComponent<StageNodeView>();
                node.OnTapped += OnStageTapped;
                go.AddComponent<ScrollRectForwarder>(); // forward drags to parent ScrollRect
                go.SetActive(false);
                _pool.Add(node);
            }

            int   count  = Mathf.Min(_stages.Length, _pool.Count);
            float rowY   = _nodeSpacingY;
            float rowOff = Game.Core.GameConfig.StageNodeRowOffset;
            float conOff = Game.Core.GameConfig.StageNodeZigzagOffset;

            // S-shape layout — groups of 4:
            //   even group: L(-rowOff), C(0), R(+rowOff), Connector(+conOff)
            //   odd  group: R(+rowOff), C(0), L(-rowOff), Connector(-conOff)
            float stagger  = rowY * 0.08f;
            float connH    = 2f * stagger + _connectorTurnGap;
            float groupH   = connH + _connectorTurnGap;

            int   lastI     = count - 1;
            int   lastG     = lastI / 4;
            int   lastP     = lastI % 4;
            float lastYBot  = lastG * groupH + (lastP == 3 ? connH : lastP * stagger);
            
            float bottomPadding = 180f; // Add bottom padding so Stage 1 is not cut off
            float totalHeight = lastYBot + _connectorTurnGap + bottomPadding;

            _contentRoot.sizeDelta = new Vector2(_contentRoot.sizeDelta.x, totalHeight);

            var positions = new Vector2[count];
            for (int i = 0; i < count; i++)
            {
                int  g       = i / 4;
                int  p       = i % 4;
                bool goRight = (g % 2 == 0);

                float rowStagger  = p < 3 ? p * stagger : 0f;
                float yFromBottom = g * groupH + (p == 3 ? connH : rowStagger) + bottomPadding;
                float y           = -(totalHeight - yFromBottom);

                float x;
                if (p < 3)
                {
                    // Row nodes: even L→C→R, odd R→C→L
                    float sign = goRight ? 1f : -1f;
                    x = (p - 1) * rowOff * sign; // p=0→-rowOff*sign, p=1→0, p=2→+rowOff*sign
                }
                else
                {
                    // Connector: swing to far edge
                    x = goRight ? conOff : -conOff;
                }

                positions[i] = new Vector2(x, y);

                var nodeRt       = _pool[i].GetComponent<RectTransform>();
                nodeRt.anchorMin = nodeRt.anchorMax = new Vector2(0.5f, 1f);
                nodeRt.pivot     = new Vector2(0.5f, 0.5f);
                nodeRt.anchoredPosition = positions[i];
            }

            foreach (var node in _chestNodes)
            {
                if (node != null) Destroy(node.gameObject);
            }
            _chestNodes.Clear();

            CreateChestNode(1, 2, positions[2], totalHeight);
            CreateChestNode(2, 5, positions[5], totalHeight);
            CreateChestNode(3, 8, positions[8], totalHeight);

            BuildPath(positions, count, totalHeight);
        }

        private void CreateChestNode(int chapterNum, int stageIndex, Vector2 stagePos, float totalHeight)
        {
            if (stageIndex >= _pool.Count) return;

            var go = Instantiate(_stageNodePrefab, _contentRoot);
            go.name = $"ChestNode_{chapterNum}";
            go.AddComponent<ScrollRectForwarder>();
            
            var node = go.GetComponent<StageNodeView>();
            var nodeRt = node.GetComponent<RectTransform>();
            nodeRt.anchorMin = nodeRt.anchorMax = new Vector2(0.5f, 1f);
            nodeRt.pivot = new Vector2(0.5f, 0.5f);

            float xOffset = stagePos.x >= 0 ? -130f : 130f;
            nodeRt.anchoredPosition = new Vector2(stagePos.x + xOffset, stagePos.y);

            var button = node.GetComponentInChildren<Button>();
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => OnChestTapped(chapterNum));
            }

            _chestNodes.Add(node);
        }

        private void RefreshChestNodes()
        {
            var progress = PlayerProgressService.Instance;
            if (progress == null) return;

            for (int i = 0; i < _chestNodes.Count; i++)
            {
                int chapterNum = i + 1;
                var node = _chestNodes[i];
                string sourceId = $"chapter{chapterNum}_chest";
                
                bool claimed = false;
                _chestClaimed.TryGetValue(sourceId, out claimed);

                int startStage = (chapterNum - 1) * 3 + 1;
                bool isCompleted = progress.GetBestStars(startStage) == 3 &&
                                   progress.GetBestStars(startStage + 1) == 3 &&
                                   progress.GetBestStars(startStage + 2) == 3;

                bool claimable = isCompleted && !claimed;

                node.Bind(0, 0, claimable || claimed, claimable);

                var label = node.GetComponentInChildren<TMPro.TMP_Text>();
                if (label != null)
                {
                    label.text = claimed ? "✓" : "🎁";
                }

                var button = node.GetComponentInChildren<Button>();
                if (button != null)
                {
                    button.interactable = claimable;
                }

                var starsRoot = node.transform.Find("Stars");
                if (starsRoot != null) starsRoot.gameObject.SetActive(false);

                node.gameObject.SetActive(true);
            }
        }

        private void OnChestTapped(int chapterNum)
        {
            string sourceId = $"chapter{chapterNum}_chest";
            
            bool claimed = false;
            _chestClaimed.TryGetValue(sourceId, out claimed);
            if (claimed) return;

            var progress = PlayerProgressService.Instance;
            if (progress == null) return;

            int startStage = (chapterNum - 1) * 3 + 1;
            bool isCompleted = progress.GetBestStars(startStage) == 3 &&
                               progress.GetBestStars(startStage + 1) == 3 &&
                               progress.GetBestStars(startStage + 2) == 3;

            if (!isCompleted)
            {
                Game.Core.UIManager.Instance?.ShowToast("3-star clear all chapter stages to unlock!", Game.Core.UI.ToastType.Warning);
                return;
            }

            Game.Core.UIManager.Instance?.ShowLoading();
            RewardsApiService.Instance?.ClaimReward(sourceId,
                onSuccess: response =>
                {
                    Game.Core.UIManager.Instance?.HideLoading();
                    _chestClaimed[sourceId] = true;
                    RefreshChestNodes();

                    System.Text.StringBuilder sb = new System.Text.StringBuilder("Chest Claimed!\n");
                    foreach (var r in response.GrantedRewards)
                    {
                        if (r.RewardType == "SOFT_CURRENCY")
                        {
                            sb.AppendLine($"+{r.Amount} Gold");
                            PlayerProgressService.Instance?.AddGold(r.Amount);
                        }
                        else if (r.RewardType == "ITEM")
                        {
                            string itemName = r.TargetId switch
                            {
                                1 => "Add Turns",
                                2 => "Bomb",
                                3 => "H Rocket",
                                4 => "Color Sweep",
                                5 => "Row Shift",
                                6 => "Cell Swap",
                                _ => $"Item {r.TargetId}"
                            };
                            sb.AppendLine($"+{r.Amount} {itemName}");
                        }
                    }
                    Game.Core.UIManager.Instance?.ShowToast(sb.ToString(), Game.Core.UI.ToastType.Success);
                },
                onError: error =>
                {
                    Game.Core.UIManager.Instance?.HideLoading();
                    Game.Core.UIManager.Instance?.ShowToast($"Failed to claim chest: {error}", Game.Core.UI.ToastType.Warning);
                }
            );
        }

        private void BuildPath(Vector2[] nodePositions, int count, float totalHeight)
        {
            var go = new GameObject("PathStrip");
            go.transform.SetParent(_contentRoot, false);
            go.transform.SetAsFirstSibling();

            _pathStrip              = go.AddComponent<UILineStrip>();
            _pathStrip.lineWidth    = _pathWidth;
            _pathStrip.color        = _pathColor;
            _pathStrip.raycastTarget = false;

            // RT: anchor (0.5,1), pivot center, centered vertically over content
            var rt              = go.GetComponent<RectTransform>();
            rt.anchorMin        = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot            = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, -totalHeight * 0.5f);
            rt.sizeDelta        = new Vector2(_contentRoot.sizeDelta.x, totalHeight);

            // Node positions are in anchor(0.5,1) space; strip local space needs +totalHeight/2 on Y
            float yOffset = totalHeight * 0.5f;
            var   curve   = SampleCatmullRom(nodePositions, count, 12);
            for (int i = 0; i < curve.Count; i++)
                curve[i] = new Vector2(curve[i].x, curve[i].y + yOffset);

            _pathStrip.SetPoints(curve);
        }

        private List<Vector2> SampleCatmullRom(Vector2[] pts, int count, int steps)
        {
            var result = new List<Vector2>();
            for (int i = 0; i < count - 1; i++)
            {
                var p0 = pts[Mathf.Max(0,         i - 1)];
                var p1 = pts[i];
                var p2 = pts[i + 1];
                var p3 = pts[Mathf.Min(count - 1, i + 2)];
                for (int s = 0; s < steps; s++)
                    result.Add(CatmullRom(p0, p1, p2, p3, (float)s / steps));
            }
            result.Add(pts[count - 1]);
            return result;
        }

        private static Vector2 CatmullRom(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
        {
            float t2 = t * t, t3 = t2 * t;
            return 0.5f * (2f * p1
                + (-p0 + p2) * t
                + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2
                + (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
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

            RefreshChestNodes();
        }

        private void RestoreScrollPosition()
        {
            if (_scrollRect != null)
                StartCoroutine(ApplyScrollNextFrame());
        }

        private IEnumerator ApplyScrollNextFrame()
        {
            yield return null;
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
            var staminaApi = StaminaApiService.Instance;
            bool canPlay = false;
            if (staminaApi != null)
            {
                if (staminaApi.LatestStamina != null && staminaApi.LatestStamina.IsUnlimited && staminaApi.GetSecondsOfUnlimitedRemaining() > 0)
                {
                    canPlay = true;
                }
                else if (staminaApi.GetEstimatedLife() > 0)
                {
                    canPlay = true;
                }
            }
            else
            {
                canPlay = true;
            }

            if (!canPlay)
            {
                ShowStaminaAdPopup();
                return;
            }

            ScrollStateCache.HomeScrollPosition = _scrollRect != null
                ? _scrollRect.verticalNormalizedPosition : 0f;
            ScrollStateCache.LastPlayedStageId = stageId;

            var transition = Game.Core.SceneTransition.Instance;
            if (transition != null)
                transition.SlideUpToScene(InGameScene);
            else
                UnityEngine.SceneManagement.SceneManager.LoadScene(InGameScene);
        }

        private void ShowStaminaAdPopup()
        {
            Game.Core.UIManager.Instance?.ShowPopup<Game.Core.UI.ConfirmDialogView>(v => v.Init(
                title: "Out of Lives",
                body: "Watch an advertisement to gain 1 Life immediately?",
                confirmLabel: "Watch Ad",
                onConfirm: () =>
                {
                    Game.Core.UIManager.Instance?.ShowLoading();
                    StaminaApiService.Instance?.ClaimAdLife("rewarded_video", "dummy_ad_token",
                        onSuccess: resp =>
                        {
                            Game.Core.UIManager.Instance?.HideLoading();
                            Game.Core.UIManager.Instance?.ShowToast("Life gained!", Game.Core.UI.ToastType.Success);
                        },
                        onError: err =>
                        {
                            Game.Core.UIManager.Instance?.HideLoading();
                            Game.Core.UIManager.Instance?.ShowToast($"Ad failed: {err}", Game.Core.UI.ToastType.Warning);
                        }
                    );
                },
                onCancel: null,
                cancelLabel: "Cancel"
            ));
        }
    }
}
