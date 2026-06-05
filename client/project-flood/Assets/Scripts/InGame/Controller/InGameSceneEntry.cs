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
        [SerializeField] private InGameController _controller;
        [SerializeField] private HUDView          _hudView;
        [SerializeField] private int              _debugStageId = 1;

        private Stage _stage;
        private int   _goldEarned;

        private const string LobbyScene  = "Lobby";
        private const string InGameScene = "InGame";

#if UNITY_EDITOR
        private static int? _overrideStageId;
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
                _overrideStageId = _debugStageId;
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            };
        }
#endif

        private void Start()
        {
#if UNITY_EDITOR
            _isFirstLoad = false;
            int stageId  = _overrideStageId ?? ScrollStateCache.LastPlayedStageId;
            if (stageId <= 0) stageId = _debugStageId;
            _overrideStageId = null;
#else
            int stageId = ScrollStateCache.LastPlayedStageId;
            if (stageId <= 0) stageId = _debugStageId;
#endif
            _stage = StageDataService.Instance?.GetStage(stageId)
                  ?? System.Array.Find(CsvLoader.Load<Stage>(Stage.ResourcePath), s => s.stage_id == stageId);

            if (_stage == null)
            {
                Debug.LogError($"[InGameSceneEntry] stage_id={stageId} not found");
                return;
            }

            _goldEarned = 0;
            _hudView?.Init(_stage.turn_limit, _stage.star1_ratio, _stage.star2_ratio);
            if (_hudView != null) _hudView.OnPausePressed += OnPausePressed;

            _controller.OnBoardUpdated      += OnBoardUpdated;
            _controller.OnContinueAvailable += OnContinueAvailable;
            _controller.OnStageEnd          += OnStageEnd;

            _controller.Init(_stage);
            StageApiService.Instance?.StartAttempt(_stage.stage_id, response =>
            {
                StaminaApiService.Instance?.FetchStamina();
            });
        }

        private void OnDestroy()
        {
            _controller.OnBoardUpdated      -= OnBoardUpdated;
            _controller.OnContinueAvailable -= OnContinueAvailable;
            _controller.OnStageEnd          -= OnStageEnd;
            if (_hudView != null) _hudView.OnPausePressed -= OnPausePressed;
        }

        private void OnBoardUpdated(int remainingTurns, float ratio)
        {
            _hudView?.UpdateTurns(remainingTurns);
            _hudView?.UpdateRatio(ratio);
        }

        private void OnContinueAvailable()
        {
            int gold = PlayerProgressService.Instance?.Gold ?? 0;
            UIManager.Instance?.ShowOverlay<FailOverlayView>(v => v.Init(
                continueCost: GameConfig.ContinueCost,
                currentGold:  gold,
                onContinue:   AcceptContinue,
                onForfeit:    () => _controller.Forfeit()));
        }

        private void AcceptContinue()
        {
            if (PlayerProgressService.Instance != null &&
                !PlayerProgressService.Instance.SpendGold(GameConfig.ContinueCost))
            {
                UIManager.Instance?.ShowToast("골드 부족", ToastType.Warning);
                return;
            }
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
                _goldEarned = CalculateGold((int)result, remainingTurns);
                PlayerProgressService.Instance?.AddGold(_goldEarned);
                PlayerProgressService.Instance?.RecordClear(_stage.stage_id, (int)result);
            }
            else
            {
                StageApiService.Instance?.FailAttempt(_stage.stage_id);
                StaminaApiService.Instance?.FetchStamina();
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
                overlay.OnRetry += () => { UIManager.Instance?.CloseOverlay(); SceneManager.LoadScene(InGameScene); };
                overlay.OnNext  += () => { UIManager.Instance?.CloseOverlay(); ScrollStateCache.LastPlayedStageId = nextId; SceneManager.LoadScene(InGameScene); };
                overlay.OnMap   += () => { UIManager.Instance?.CloseOverlay(); GoToLobby(); };

                if (!fail && StageApiService.Instance != null && StageApiService.Instance.HasAttemptFor(_stage.stage_id))
                {
                    var request = _controller.BuildClearRequest(System.Guid.NewGuid().ToString("N"));
                    StageApiService.Instance.ClearAttempt(_stage.stage_id, request, response =>
                    {
                        overlay.SetServerRank(response.StageRank, response.IsNewBest);
                        StaminaApiService.Instance?.FetchStamina();
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

        private static int CalculateGold(int stars, int remainingTurns)
        {
            int base_ = stars switch { 3 => 150, 2 => 100, 1 => 70, _ => 0 };
            return base_ + remainingTurns * 5;
        }
    }
}
