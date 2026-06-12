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
        [SerializeField] private GameObject    _chestPrefab;
        [SerializeField] private float         _nodeSpacingY      = 200f;
        [SerializeField] private float         _connectorTurnGap = 375f; // Y gap: row-end→connector and connector→next-row
        [SerializeField] private Color         _pathColor    = new Color(0.5f, 0.7f, 1f, 0.5f);
        [SerializeField] private float         _pathWidth    = 12f;
        [SerializeField] private Sprite        _guideOrbSprite;

        private const float OverdrawBuffer = 400f;

                private readonly List<StageNodeView>       _pool          = new List<StageNodeView>();
        private readonly List<UILineStrip>         _pathStrips    = new List<UILineStrip>();
        private readonly Dictionary<int, Vector2>  _stagePositions = new Dictionary<int, Vector2>();
        private Coroutine                    _guideOrbCoroutine;
        private Image                        _guideOrb;
        private Stage[] _stages;
        private int     _currentStageId;

        private readonly List<ChapterChestView>     _chestNodes = new List<ChapterChestView>();
        private readonly List<ChapterBackgroundView> _bgViews    = new List<ChapterBackgroundView>();
        private GameObject                           _bgGradientGo;
        private readonly Dictionary<string, bool> _chestClaimed = new Dictionary<string, bool>
        {
            { "chapter1_chest", false },
            { "chapter2_chest", false },
            { "chapter3_chest", false },
        };

        private const string InGameScene = "InGame";

        private void Awake()
        {
            if (_contentRoot == null && _scrollRect != null)
                _contentRoot = _scrollRect.content;

            if (_chestPrefab == null)
                _chestPrefab = Resources.Load<GameObject>("Prefabs/UI/ChapterChest");

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
            _scrollRect.onValueChanged.AddListener(OnScrolled);

            // Immediate render if layout already built (tab re-enter); otherwise ApplyScrollNextFrame handles it
            if (_scrollRect.viewport.rect.height > 0f)
                OnScrolled(new Vector2(0f, _scrollRect.verticalNormalizedPosition));

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
                    RefreshChestNodes();
                }, err => Debug.LogWarning($"[HomeTabView] failed to fetch reward sources: {err}"));
            }
        }

        private void OnDisable()
        {
            ScrollStateCache.HomeScrollPosition = _scrollRect != null
                ? _scrollRect.verticalNormalizedPosition : 0f;

            if (_scrollRect != null)
                _scrollRect.onValueChanged.RemoveListener(OnScrolled);

            if (_guideOrbCoroutine != null)
            {
                StopCoroutine(_guideOrbCoroutine);
                _guideOrbCoroutine = null;
            }
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

            foreach (var ps in _pathStrips)
            {
                if (ps != null) Destroy(ps.gameObject);
            }
            _pathStrips.Clear();

            // Clean up editor dummy placeholder nodes so they don't overlap at runtime
            for (int i = _contentRoot.childCount - 1; i >= 0; i--)
            {
                var child = _contentRoot.GetChild(i);
                if (child.gameObject.name.StartsWith("StageNode_"))
                {
                    Destroy(child.gameObject);
                }
            }

            if (_bgGradientGo != null) { Destroy(_bgGradientGo); _bgGradientGo = null; }
            foreach (var bg in _bgViews)
                if (bg != null) Destroy(bg.gameObject);
            _bgViews.Clear();

            // Deactivate all pool nodes — prevents stale 0-position nodes on re-enable
            foreach (var n in _pool) n.gameObject.SetActive(false);
            _stagePositions.Clear();

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

            // stageCount drives ALL layout/position math — decoupled from pool size
            int   stageCount = _stages.Length;
            float rowY   = _nodeSpacingY;
            float rowOff = Game.Core.GameConfig.StageNodeRowOffset;
            float conOff = Game.Core.GameConfig.StageNodeZigzagOffset;

            // S-shape layout — groups of 4:
            //   even group: L(-rowOff), C(0), R(+rowOff), Connector(+conOff)
            //   odd  group: R(+rowOff), C(0), L(-rowOff), Connector(-conOff)
            float stagger  = rowY * 0.08f;
            float connH    = 2f * stagger + _connectorTurnGap;
            float groupH   = connH + _connectorTurnGap;

            int   lastI     = stageCount - 1;
            int   lastG     = lastI / 4;
            int   lastP     = lastI % 4;
            float lastYBot  = lastG * groupH + (lastP == 3 ? connH : lastP * stagger);

            float bottomPadding = 180f; // Add bottom padding so Stage 1 is not cut off
            float totalHeight = lastYBot + _connectorTurnGap + bottomPadding;

            _contentRoot.sizeDelta = new Vector2(_contentRoot.sizeDelta.x, totalHeight);

            var positions = new Vector2[stageCount];
            for (int i = 0; i < stageCount; i++)
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
                _stagePositions[_stages[i].stage_id] = positions[i];
            }

            foreach (var node in _chestNodes)
            {
                if (node != null) Destroy(node.gameObject);
            }
            _chestNodes.Clear();

            var chapterLastIdx = new Dictionary<int, int>();
            for (int i = 0; i < stageCount; i++)
                chapterLastIdx[_stages[i].chapter_id] = i;

            var sortedChapters = new List<int>(chapterLastIdx.Keys);
            sortedChapters.Sort();
            foreach (int cid in sortedChapters)
                CreateChestNode(cid, positions[chapterLastIdx[cid]], totalHeight);

            BuildPath(positions, stageCount, totalHeight);
            StartGuideOrb(positions, stageCount);
            BuildChapterBackgrounds(positions, stageCount, totalHeight);
            RefreshChestNodes();
        }

        private void CreateChestNode(int chapterNum, Vector2 stagePos, float totalHeight)
        {
            if (_chestPrefab == null) return;

            var go = Instantiate(_chestPrefab, _contentRoot);
            go.name = $"ChestNode_{chapterNum}";
            go.AddComponent<ScrollRectForwarder>();
            
            var chestView = go.GetComponent<ChapterChestView>();
            var nodeRt = chestView.GetComponent<RectTransform>();
            nodeRt.anchorMin = nodeRt.anchorMax = new Vector2(0.5f, 1f);
            nodeRt.pivot = new Vector2(0.5f, 0.5f);

            // Reposition chest to chapter clear top margins, away from stage nodes
            float targetX = stagePos.x >= 0 ? 260f : -260f;
            nodeRt.anchoredPosition = new Vector2(targetX, stagePos.y + 110f);

            var button = chestView.GetComponentInChildren<Button>();
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => OnChestTapped(chapterNum));
            }

            _chestNodes.Add(chestView);
        }

        private bool IsChapterAllThreeStars(int chapterNum)
        {
            var progress = PlayerProgressService.Instance;
            if (progress == null || _stages == null) return false;
            bool hasStages = false;
            foreach (var s in _stages)
            {
                if (s.chapter_id != chapterNum) continue;
                hasStages = true;
                if (progress.GetBestStars(s.stage_id) < 3) return false;
            }
            return hasStages;
        }

        private void RefreshChestNodes()
        {
            for (int i = 0; i < _chestNodes.Count; i++)
            {
                int chapterNum = i + 1;
                var chestView = _chestNodes[i];
                string sourceId = $"chapter{chapterNum}_chest";

                _chestClaimed.TryGetValue(sourceId, out bool claimed);

                ChapterChestView.ChestState state = ChapterChestView.ChestState.Inactive;
                if (claimed)
                    state = ChapterChestView.ChestState.Claimed;
                else if (IsChapterAllThreeStars(chapterNum))
                    state = ChapterChestView.ChestState.Active;

                chestView.SetState(state);
                chestView.gameObject.SetActive(true);
            }
        }

        private void OnChestTapped(int chapterNum)
        {
            string sourceId = $"chapter{chapterNum}_chest";
            
            bool claimed = false;
            _chestClaimed.TryGetValue(sourceId, out claimed);
            if (claimed) return;

            if (PlayerProgressService.Instance == null) return;

            if (!IsChapterAllThreeStars(chapterNum))
            {
                Game.Core.UIManager.Instance?.ShowToast(LocalizationService.Instance.Get("toast.chapter_unlock_requirement"), Game.Core.UI.ToastType.Warning);
                return;
            }

            Game.Core.UIManager.Instance?.ShowLoading();
            RewardsApiService.Instance?.ClaimReward(sourceId,
                onSuccess: response =>
                {
                    Game.Core.UIManager.Instance?.HideLoading();
                    _chestClaimed[sourceId] = true;
                    RefreshChestNodes();

                    var loc = LocalizationService.Instance;
                    System.Text.StringBuilder sb = new System.Text.StringBuilder(loc.Get("toast.chest_claimed") + "\n");
                    foreach (var r in response.GrantedRewards)
                    {
                        if (r.RewardType == "SOFT_CURRENCY")
                        {
                            sb.AppendLine($"+{r.Amount} {loc.Get("common.gold")}");
                            PlayerProgressService.Instance?.AddGold(r.Amount);
                        }
                        else if (r.RewardType == "ITEM")
                        {
                            string itemName = r.TargetId switch
                            {
                                1 => loc.Get("item.name.add_turn"),
                                2 => loc.Get("item.name.bomb"),
                                3 => loc.Get("item.name.h_rocket"),
                                4 => loc.Get("item.name.color_sweep"),
                                5 => loc.Get("item.name.row_shift"),
                                6 => loc.Get("item.name.cell_swap"),
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
                    Game.Core.UIManager.Instance?.ShowToast(LocalizationService.Instance.Get("toast.chest_claim_failed"), Game.Core.UI.ToastType.Warning);
                }
            );
        }

        private void BuildChapterBackgrounds(Vector2[] positions, int count, float totalHeight)
        {
            var chapters = new Dictionary<int, (int first, int last)>();
            for (int i = 0; i < count; i++)
            {
                int cid = _stages[i].chapter_id;
                if (!chapters.TryGetValue(cid, out var range))
                    chapters[cid] = (i, i);
                else
                    chapters[cid] = (range.first, i);
            }

            const float nodeHalf = 100f;
            const float chPad    = 160f;

            var sortedIds = new List<int>(chapters.Keys);
            sortedIds.Sort();

            var bounds = new Dictionary<int, (float yTop, float yBot)>();
            foreach (int cid in sortedIds)
            {
                var r = chapters[cid];
                bounds[cid] = (
                    positions[r.last].y  + nodeHalf + chPad,
                    positions[r.first].y - nodeHalf - chPad
                );
            }

            // ── Single multi-stop gradient spanning ALL chapters ──────────────
            // No seam strips needed: stops computed from chapter bounds ensure
            // the gradient blends exactly at each chapter boundary.
            // t = 0 (bottom of content) → t = 1 (top of content)
            // t(y) = (totalHeight + y) / totalHeight  (y is negative anchor-top space)
            {
                var stopList = new List<(float t, Color color)>();
                for (int c = 0; c < sortedIds.Count; c++)
                {
                    int   cid   = sortedIds[c];
                    var   theme = ChapterBgTheme.Get(cid);
                    var   (yTop, yBot) = bounds[cid];
                    float tBot  = (totalHeight + yBot) / totalHeight;
                    float tTop  = (totalHeight + yTop) / totalHeight;
                    tBot = Mathf.Clamp01(tBot);
                    tTop = Mathf.Clamp01(tTop);
                    stopList.Add((tBot, theme.BottomColor));
                    stopList.Add((tTop, theme.TopColor));
                }

                var gradGo = new GameObject("ChapterGradient", typeof(RectTransform));
                gradGo.transform.SetParent(_contentRoot, false);
                var grad = gradGo.AddComponent<UIVerticalGradient>();
                grad.raycastTarget = false;
                grad.SetStops(stopList.ToArray());
                var grt = gradGo.GetComponent<RectTransform>();
                grt.anchorMin = Vector2.zero;
                grt.anchorMax = Vector2.one;
                grt.offsetMin = grt.offsetMax = Vector2.zero;
                gradGo.transform.SetSiblingIndex(0); // bottom of render stack
                _bgGradientGo = gradGo;
            }

            // ── Per-chapter decoration views (on top of gradient, behind PathStrip) ──
            // Create in reverse so ch1 dec index < ch2 dec index (ch1 renders behind ch2 at seam)
            for (int c = sortedIds.Count - 1; c >= 0; c--)
            {
                int   cid          = sortedIds[c];
                var   (yTop, yBot) = bounds[cid];
                float height       = yTop - yBot;

                var go = new GameObject($"ChapterBg_{cid}", typeof(RectTransform));
                go.transform.SetParent(_contentRoot, false);
                go.AddComponent<ScrollRectForwarder>();
                var bgView = go.AddComponent<ChapterBackgroundView>();
                bgView.Bind(chapterId: cid, bgThemeId: cid, yAnchoredTop: yTop, height: height);
                go.transform.SetSiblingIndex(1); // after gradient (index 0), before PathStrip
                _bgViews.Add(bgView);
            }
        }

        private void BuildPath(Vector2[] nodePositions, int count, float totalHeight)
        {
            foreach (var ps in _pathStrips)
            {
                if (ps != null) Destroy(ps.gameObject);
            }
            _pathStrips.Clear();

            var chapters = new Dictionary<int, (int first, int last)>();
            for (int i = 0; i < count; i++)
            {
                int cid = _stages[i].chapter_id;
                if (!chapters.TryGetValue(cid, out var range))
                    chapters[cid] = (i, i);
                else
                    chapters[cid] = (range.first, i);
            }

            var sortedIds = new List<int>(chapters.Keys);
            sortedIds.Sort();

            float yOffset = totalHeight * 0.5f;
            var progress = PlayerProgressService.Instance;

            foreach (int cid in sortedIds)
            {
                var range = chapters[cid];
                int startIdx = range.first;
                
                // Connect to next chapter's first node for continuous lines
                int endIdx = range.last;
                if (endIdx < count - 1)
                {
                    endIdx++;
                }

                int segCount = endIdx - startIdx + 1;
                if (segCount < 2) continue;

                var chapterPts = new Vector2[segCount];
                for (int s = 0; s < segCount; s++)
                {
                    chapterPts[s] = nodePositions[startIdx + s];
                }

                var curve = SampleCatmullRom(chapterPts, segCount, 12);
                for (int s = 0; s < curve.Count; s++)
                {
                    curve[s] = new Vector2(curve[s].x, curve[s].y + yOffset);
                }

                var go = new GameObject($"PathStrip_Chapter_{cid}");
                go.transform.SetParent(_contentRoot, false);
                go.transform.SetAsFirstSibling();

                var pathStrip = go.AddComponent<UILineStrip>();
                var theme = ChapterBgTheme.Get(cid);

                pathStrip.lineWidth = theme.PathWidth;
                pathStrip.scrollSpeed = theme.PathScrollSpeed;
                pathStrip.useOutline = true;
                pathStrip.outlineWidth = 4f;
                pathStrip.outlineColor = new Color(0f, 0.05f, 0.15f, 0.65f);
                pathStrip.raycastTarget = false;

                Texture2D customTex = null;
                if (!string.IsNullOrEmpty(theme.PathResourceKey))
                {
                    string resourcePath = $"Sprites/Path/{theme.PathResourceKey}";
                    customTex = Resources.Load<Texture2D>(resourcePath);
                    if (customTex != null)
                        Debug.Log($"[HomeTabView] Loaded path texture for chapter {cid} from Resources/{resourcePath}");
                    else
                        Debug.LogWarning($"[HomeTabView] path texture '{theme.PathResourceKey}' not found for chapter {cid}. Trying path_chapter fallback.");
                }

                if (customTex == null)
                {
                    customTex = Resources.Load<Texture2D>("Sprites/Path/path_chapter");
                    if (customTex != null)
                        Debug.Log($"[HomeTabView] Loaded fallback path texture 'path_chapter' for chapter {cid}");
                }

                if (customTex != null)
                {
                    pathStrip.SetTexture(customTex);
                    pathStrip.color = Color.white;
                    pathStrip.textureTiling = totalHeight / (theme.PathWidth * 8f);
                }
                else
                {
                    // Fallback procedural dashed path style
                    pathStrip.color = theme.PathColor;
                    pathStrip.useProceduralDashes = true;
                    pathStrip.dashLength = 40f;
                    pathStrip.gapLength = 20f;
                    pathStrip.textureTiling = 10f;
                    pathStrip.scrollSpeed = theme.PathScrollSpeed * 10f; // Scale speed for procedural dash animation
                }

                var rt = go.GetComponent<RectTransform>();
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(0f, -totalHeight * 0.5f);
                rt.sizeDelta = new Vector2(_contentRoot.sizeDelta.x, totalHeight);

                pathStrip.SetPoints(curve);
                _pathStrips.Add(pathStrip);

                ApplyChapterPathStyle(pathStrip, cid, startIdx, endIdx, progress);
            }
        }

        private void ApplyChapterPathStyle(UILineStrip pathStrip, int chapterId, int startIdx, int endIdx, PlayerProgressService progress)
        {
            if (progress == null) return;

            bool isChapterLocked = true;
            bool isChapterFullyCleared = true;

            for (int idx = startIdx; idx <= endIdx; idx++)
            {
                if (idx >= _stages.Length) break;
                var stageId = _stages[idx].stage_id;
                
                if (progress.IsStageUnlocked(stageId))
                {
                    isChapterLocked = false;
                }
                
                if (progress.GetBestStars(stageId) == 0)
                {
                    isChapterFullyCleared = false;
                }
            }

            if (isChapterLocked)
            {
                pathStrip.color = new Color(0.4f, 0.4f, 0.4f, 0.15f);
                pathStrip.scrollSpeed = 0f;
            }
            else if (isChapterFullyCleared)
            {
                var theme = ChapterBgTheme.Get(chapterId);
                pathStrip.color = new Color(theme.PathColor.r, theme.PathColor.g, theme.PathColor.b, 0.55f);
                pathStrip.scrollSpeed = theme.PathScrollSpeed * 0.5f;
            }
            else
            {
                var theme = ChapterBgTheme.Get(chapterId);
                pathStrip.color = theme.PathColor;
                pathStrip.scrollSpeed = theme.PathScrollSpeed;
            }
        }

        private void StartGuideOrb(Vector2[] positions, int count)
        {
            if (_guideOrbCoroutine != null) { StopCoroutine(_guideOrbCoroutine); _guideOrbCoroutine = null; }
            if (_guideOrb != null) { Destroy(_guideOrb.gameObject); _guideOrb = null; }

            int currentIdx = _currentStageId - 1;
            if (currentIdx < 0 || currentIdx >= count) return;

            var go = new GameObject("GuideOrb", typeof(Image));
            go.transform.SetParent(_contentRoot, false);
            _guideOrb = go.GetComponent<Image>();
            _guideOrb.raycastTarget = false;

            if (_guideOrbSprite != null)
            {
                _guideOrb.sprite = _guideOrbSprite;
            }

            var cid = _stages[currentIdx].chapter_id;
            var theme = ChapterBgTheme.Get(cid);
            _guideOrb.color = theme.PathColor;

            var rt = _guideOrb.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(24f, 24f);

            if (currentIdx == 0)
            {
                rt.anchoredPosition = positions[0];
                _guideOrbCoroutine = StartCoroutine(OrbPulseRoutine(rt));
            }
            else
            {
                Vector2 startPos = positions[currentIdx - 1];
                Vector2 endPos = positions[currentIdx];
                _guideOrbCoroutine = StartCoroutine(OrbTravelRoutine(rt, startPos, endPos));
            }
        }

        private IEnumerator OrbPulseRoutine(RectTransform rt)
        {
            while (true)
            {
                float s = 1.0f + Mathf.PingPong(Time.time * 2f, 0.4f);
                rt.localScale = new Vector3(s, s, 1f);
                
                float alpha = 0.5f + 0.5f * Mathf.PingPong(Time.time * 2f, 0.5f);
                _guideOrb.color = new Color(_guideOrb.color.r, _guideOrb.color.g, _guideOrb.color.b, alpha);
                yield return null;
            }
        }

        private IEnumerator OrbTravelRoutine(RectTransform rt, Vector2 start, Vector2 end)
        {
            Vector2 mid = Vector2.Lerp(start, end, 0.5f);
            Vector2 dir = (end - start).normalized;
            Vector2 perp = new Vector2(-dir.y, dir.x) * 35f;
            Vector2 curveControl = mid + perp;

            while (true)
            {
                float t = 0f;
                while (t < 1.0f)
                {
                    t += Time.deltaTime * 0.7f;
                    float tc = Mathf.Clamp01(t);

                    Vector2 m1 = Vector2.Lerp(start, curveControl, tc);
                    Vector2 m2 = Vector2.Lerp(curveControl, end, tc);
                    rt.anchoredPosition = Vector2.Lerp(m1, m2, tc);

                    float s = 1.0f + 0.35f * Mathf.Sin(tc * Mathf.PI);
                    rt.localScale = new Vector3(s, s, 1f);

                    yield return null;
                }
                yield return new WaitForSeconds(0.4f);
            }
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

        private void OnScrolled(Vector2 scrollVal)
        {
            if (_stages == null || _pool.Count == 0) return;

            float viewportH = _scrollRect.viewport.rect.height;
            if (viewportH <= 0f)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(_scrollRect.viewport);
                viewportH = _scrollRect.viewport.rect.height;
                if (viewportH <= 0f) return;
            }

            float totalH     = _contentRoot.sizeDelta.y;
            float scrollable = Mathf.Max(0f, totalH - viewportH);
            float offset     = Mathf.Clamp01(1f - scrollVal.y) * scrollable;

            // visible Y range in anchor-top space (y is negative going down from top)
            float visTop = -(offset - OverdrawBuffer);
            float visBot = -(offset + viewportH + OverdrawBuffer);

            var progress = PlayerProgressService.Instance;
            int  poolIdx = 0;

            for (int i = 0; i < _stages.Length && poolIdx < _pool.Count; i++)
            {
                if (!_stagePositions.TryGetValue(_stages[i].stage_id, out Vector2 nodePos)) continue;
                if (nodePos.y > visTop || nodePos.y < visBot) continue;

                var node = _pool[poolIdx++];
                var s    = _stages[i];
                int  stars  = progress?.GetBestStars(s.stage_id) ?? 0;
                bool unlock = progress?.IsStageUnlocked(s.stage_id) ?? (s.stage_id == 1);
                bool cur    = s.stage_id == _currentStageId;
                node.Bind(s.stage_id, stars, unlock, cur, s.chapter_id, (int)s.difficulty);
                var rt      = node.GetComponent<RectTransform>();
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
                rt.pivot     = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = nodePos;
                node.gameObject.SetActive(true);
            }

            for (; poolIdx < _pool.Count; poolIdx++)
                _pool[poolIdx].gameObject.SetActive(false);

            // Pause animations for off-screen chapter backgrounds (coroutine CPU saving)
            const float bgBuffer = 350f;
            foreach (var bg in _bgViews)
            {
                if (bg == null) continue;
                bool inView = bg.YTop >= visBot - bgBuffer && bg.YBot <= visTop + bgBuffer;
                if (bg.enabled != inView) bg.enabled = inView;
            }
        }

        private void RestoreScrollPosition()
        {
            if (_scrollRect != null)
                StartCoroutine(ApplyScrollNextFrame());
        }

        private IEnumerator ApplyScrollNextFrame()
        {
            yield return null;
            LayoutRebuilder.ForceRebuildLayoutImmediate(_scrollRect.viewport);

            int targetId = ScrollStateCache.LastPlayedStageId;
            if (targetId > 0)
            {
                ScrollStateCache.LastPlayedStageId = 0;
                if (_stagePositions.TryGetValue(targetId, out Vector2 nodePos))
                {
                    float totalH     = _contentRoot.sizeDelta.y;
                    float viewportH  = _scrollRect.viewport.rect.height;
                    float scrollable = totalH - viewportH;
                    _scrollRect.verticalNormalizedPosition = scrollable > 0f
                        ? Mathf.Clamp01(1f + (nodePos.y + viewportH * 0.5f) / scrollable)
                        : 1f;
                    OnScrolled(new Vector2(0f, _scrollRect.verticalNormalizedPosition));
                    yield break;
                }
            }
            _scrollRect.verticalNormalizedPosition = ScrollStateCache.HomeScrollPosition;
            OnScrolled(new Vector2(0f, _scrollRect.verticalNormalizedPosition));
        }

        private void OnStageTapped(int stageId)
        {
            var stage = StageDataService.Instance?.GetStage(stageId);
            if (stage == null) return;

            var progress  = PlayerProgressService.Instance;
            int stars     = progress?.GetBestStars(stageId) ?? 0;
            bool isLocked = !(progress?.IsStageUnlocked(stageId) ?? (stageId == 1));

            Game.Core.UIManager.Instance?.ShowPopup<StageInfoPopupView>(v => v.Init(
                stageId:    stageId,
                bestStars:  stars,
                bestMoves:  0,
                onPlay:     () => EnterStage(stageId),
                difficulty: (int)stage.difficulty,
                isLocked:   isLocked));
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
                title: LocalizationService.Instance.Get("popup.stamina.out_of_lives"),
                body: LocalizationService.Instance.Get("popup.stamina.watch_ad_body"),
                confirmLabel: LocalizationService.Instance.Get("popup.fail.watch_ad"),
                onConfirm: () =>
                {
                    Game.Core.UIManager.Instance?.ShowLoading();
                    StaminaApiService.Instance?.ClaimAdLife("rewarded_video", "dummy_ad_token",
                        onSuccess: resp =>
                        {
                            Game.Core.UIManager.Instance?.HideLoading();
                            Game.Core.UIManager.Instance?.ShowToast(LocalizationService.Instance.Get("toast.life_gained"), Game.Core.UI.ToastType.Success);
                        },
                        onError: err =>
                        {
                            Game.Core.UIManager.Instance?.HideLoading();
                            Game.Core.UIManager.Instance?.ShowToast(LocalizationService.Instance.Get("error.ad_failed"), Game.Core.UI.ToastType.Warning);
                        }
                    );
                },
                onCancel: null,
                cancelLabel: LocalizationService.Instance.Get("common.btn_cancel")
            ));
        }
    }
}
