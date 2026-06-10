using Game.Core;
using Game.Core.UI;
using Game.InGame.Board;
using Game.InGame.View;
using Game.OutGame.Lobby;
using Game.OutGame.Settings;
using Game.Services;
using Game.Utils;
using ProjectFlood.Data.Generated;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.InGame.Controller
{
    public class InGameSceneEntry : MonoBehaviour
    {
        [SerializeField] private InGameController           _controller;
        [SerializeField] private HUDView                    _hudView;
        [SerializeField] private InGameSceneBackgroundView  _sceneBg;
        [SerializeField] private int                        _debugStageId = 1;

        private Stage _stage;
        private int   _goldEarned;

        private const string LobbyScene  = "Lobby";
        private const string InGameScene = "InGame";

#if UNITY_EDITOR
        private static bool _reloadQueued;
        private static bool _isFirstLoad = true;

        [UnityEditor.InitializeOnLoadMethod]
        static void Init()
        {
            UnityEditor.EditorApplication.playModeStateChanged += state =>
            {
                if (state == UnityEditor.PlayModeStateChange.ExitingEditMode)
                    _isFirstLoad = true;
            };
        }

        private void OnValidate()
        {
            if (!Application.isPlaying || _reloadQueued || _isFirstLoad) return;
            _reloadQueued = true;
            UnityEditor.EditorApplication.delayCall += () =>
            {
                _reloadQueued    = false;
                if (!Application.isPlaying) return;
                _isFirstLoad     = true;
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            };
        }
#endif

        private void Start()
        {
            int stageId = ScrollStateCache.LastPlayedStageId;

            // Final safety fallback
            if (stageId <= 0)
            {
                stageId = _debugStageId > 0 ? _debugStageId : 1;
                ScrollStateCache.LastPlayedStageId = stageId;
                Debug.Log($"[InGameSceneEntry] LastPlayedStageId was invalid, using debug fallback: {stageId}");
            }

            Debug.Log($"[InGameSceneEntry] Final Resolved StageId={stageId}");
            _stage = StageDataService.Instance?.GetStage(stageId)
                  ?? System.Array.Find(CsvLoader.Load<Stage>(Stage.ResourcePath), s => s.stage_id == stageId);

            if (_stage == null)
            {
                Debug.LogError($"[InGameSceneEntry] stage_id={stageId} not found");
                return;
            }

            _goldEarned = 0;

            var chapters  = CsvLoader.Load<Chapter>(Chapter.ResourcePath);
            var chapterRow = System.Array.Find(chapters, c => c.chapter_id == _stage.chapter_id);
            _sceneBg?.Bind(chapterRow != null ? (int)chapterRow.bg_theme_id : 1);

            int extraTurns = 0;
            if (ScrollStateCache.UseExtraTurnsItem)
            {
                extraTurns = 3;
                ScrollStateCache.UseExtraTurnsItem = false;
            }

            _controller.OnBoardUpdated      += OnBoardUpdated;
            _controller.OnContinueAvailable += OnContinueAvailable;
            _controller.OnStageEnd          += OnStageEnd;

            _controller.Init(_stage, extraTurns);
            _hudView?.Init(_stage.turn_limit + extraTurns, _controller.RemainingCells);
            if (_hudView != null) _hudView.OnPausePressed += OnPausePressed;
            
            StageApiService.Instance?.StartAttempt(_stage.stage_id, response =>
            {
                StaminaApiService.Instance?.UpdateStamina(response.Stamina, response.ServerTime);
                ScrollStateCache.CurrentWinStreak = response.WinStreak;
            }, error =>
            {
                if (error == "INSUFFICIENT_STAMINA")
                {
                    UIManager.Instance?.ShowPopup<OutGame.Lobby.StaminaPopupView>();
                    GoToLobby();
                }
                else if (error == "STAGE_LOCKED")
                {
                    UIManager.Instance?.ShowToast("Stage is locked!", ToastType.Warning);
                    GoToLobby();
                }
                else
                {
                    Debug.LogWarning($"[InGameSceneEntry] StartAttempt failed: {error}");
                }
            });
        }

        private void OnDestroy()
        {
            _controller.OnBoardUpdated      -= OnBoardUpdated;
            _controller.OnContinueAvailable -= OnContinueAvailable;
            _controller.OnStageEnd          -= OnStageEnd;
            if (_hudView != null) _hudView.OnPausePressed -= OnPausePressed;
        }

        private void OnBoardUpdated(int remainingTurns, int remainingCells)
        {
            _hudView?.UpdateTurns(remainingTurns);
            _hudView?.UpdateRemainingCells(remainingCells);
        }

        private void OnContinueAvailable()
        {
            int gold = PlayerProgressService.Instance?.Gold ?? 0;
            UIManager.Instance?.ShowOverlay<FailOverlayView>(v => v.Init(
                continueCost: GameConfig.ContinueCost,
                currentGold:  gold,
                onContinue:   AcceptContinue,
                onForfeit:    () => _controller.Forfeit(),
                onReviveSuccess: extraTurns =>
                {
                    UIManager.Instance?.CloseOverlay();
                    _controller.Continue(extraTurns);
                }));
        }

        private void AcceptContinue()
        {
            var progress = PlayerProgressService.Instance;
            if (progress != null && !progress.CanAfford(GameConfig.ContinueCost))
            {
                UIManager.Instance?.ShowToast("골드 부족", ToastType.Warning);
                return;
            }
            progress?.SpendGold(GameConfig.ContinueCost);
            CurrencyApiService.Instance?.SpendGold(GameConfig.ContinueCost, "continue",
                onSuccess: snap => PlayerProgressService.Instance?.SetGold((int)snap.SoftAmount),
                onError: err => Debug.LogWarning($"[InGame] currency spend sync failed: {err}"));
            UIManager.Instance?.CloseOverlay();
            _controller.Continue(GameConfig.ContinueExtraTurns);
        }

        private void OnStageEnd(StarResult result, int remainingTurns)
        {
            bool fail      = result == StarResult.Fail;
            int  turnsUsed = _stage.turn_limit - remainingTurns;
            int  nextId    = _stage.stage_id + 1;

            if (!fail)
            {
                bool isFirstClear = (PlayerProgressService.Instance?.GetBestStars(_stage.stage_id) ?? 0) == 0;
                if (isFirstClear)
                {
                    _goldEarned = CalculateGold(_stage);
                    PlayerProgressService.Instance?.AddGold(_goldEarned);
                }
                PlayerProgressService.Instance?.RecordClear(_stage.stage_id, (int)result);
                PlayerPrefs.DeleteKey("stage_fail_count_" + _stage.stage_id);
                PlayerPrefs.Save();
            }
            // Note: _goldEarned is for UI display only; server gold is reconciled from ClearAttempt response below.
            else
            {
                StageApiService.Instance?.FailAttempt(_stage.stage_id);
                StaminaApiService.Instance?.FetchStamina();
                int failCount = PlayerPrefs.GetInt("stage_fail_count_" + _stage.stage_id, 0) + 1;
                PlayerPrefs.SetInt("stage_fail_count_" + _stage.stage_id, failCount);
                PlayerPrefs.Save();
                Services.Tutorial.TutorialManager.Instance?.CheckFailTriggers(_stage.stage_id, failCount);
            }

            float ratio     = _controller.ComputeRatioPublic();
            bool  nextLocked = StageDataService.Instance?.GetStage(nextId) == null
                             || !(PlayerProgressService.Instance?.IsStageUnlocked(nextId) ?? false);

            UIManager.Instance?.ShowOverlay<ResultOverlayView>(v => v.Init(
                result:         result,
                stageId:        _stage.stage_id,
                turnsUsed:      turnsUsed,
                totalTurns:     _stage.turn_limit,
                clearanceRatio: ratio,
                goldEarned:     _goldEarned,
                nextLocked:     nextLocked));

            var overlay = UIManager.Instance?.GetCurrentOverlay<ResultOverlayView>();
            if (overlay != null)
            {
                overlay.OnRetry += () => 
                { 
                    UIManager.Instance?.CloseOverlay(); 
                    ScrollStateCache.LastPlayedStageId = _stage.stage_id;
                    SceneManager.LoadScene(InGameScene); 
                };
                overlay.OnNext  += () => 
                { 
                    UIManager.Instance?.CloseOverlay(); 
                    ScrollStateCache.LastPlayedStageId = nextId;
                    SceneManager.LoadScene(InGameScene); 
                };
                overlay.OnMap   += () => { UIManager.Instance?.CloseOverlay(); GoToLobby(); };

                if (!fail && StageApiService.Instance != null && StageApiService.Instance.HasAttemptFor(_stage.stage_id))
                {
                    var request = _controller.BuildClearRequest(System.Guid.NewGuid().ToString("N"));
                    StageApiService.Instance.ClearAttempt(_stage.stage_id, request, response =>
                    {
                        overlay.SetServerRank(response.StageRank, response.IsNewBest);
                        StaminaApiService.Instance?.FetchStamina();
                        if (response.Currency != null)
                            PlayerProgressService.Instance?.SetGold((int)response.Currency.SoftAmount);
                    }, error => Debug.LogWarning($"[InGameSceneEntry] stage clear sync failed: {error}"));
                }
            }
        }

        private void OnPausePressed()
        {
            UIManager.Instance?.ShowOverlay<PausePopupView>(v =>
            {
                v.OnRestart     += () => { UIManager.Instance?.CloseOverlay(); SceneManager.LoadScene(InGameScene); };
                v.OnSettings    += () => UIManager.Instance?.ShowPopup<SettingsPanelView>();
                v.OnStageSelect += () => { UIManager.Instance?.CloseOverlay(); GoToLobby(); };
            });
        }

        private static void GoToLobby()
        {
            var transition = SceneTransition.Instance;
            if (transition != null) transition.SlideDownToScene(LobbyScene);
            else SceneManager.LoadScene(LobbyScene);
        }

        private static int CalculateGold(Stage stage)
        {
            var items = CsvLoader.Load<ProjectFlood.Data.Generated.RewardItem>(
                ProjectFlood.Data.Generated.RewardItem.ResourcePath);
            if (items == null) return 0;
            foreach (var item in items)
                if (item.reward_group_id == stage.reward_group_id && item.reward_type == "SOFT_CURRENCY")
                    return item.amount;
            return 0;
        }
    }
}
