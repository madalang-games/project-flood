using System;
using System.Collections;
using System.Collections.Generic;
using Game.InGame.Board;
using Game.InGame.View;
using Game.Services;
using ProjectFlood.Contracts.GameTypes;
using ProjectFlood.Data.Generated;
using UnityEngine;

namespace Game.Services.Tutorial
{
    public class TutorialManager : MonoBehaviour
    {
        private static TutorialManager _instance;
        public static TutorialManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("TutorialManager");
                    _instance = go.AddComponent<TutorialManager>();
                }
                return _instance;
            }
        }

        private TutorialStep[] _allSteps;
        private HashSet<int> _completedTutorialIds = new HashSet<int>();
        private TutorialStepSequencer _sequencer = new TutorialStepSequencer();
        private int _activeGroupId = -1;

        public bool IsBlocking => _sequencer.IsActive && _sequencer.CurrentStep != null && _sequencer.CurrentStep.is_blocking;
        public bool IsActive => _sequencer.IsActive;
        public TutorialStep CurrentStep => _sequencer.CurrentStep;

        public event Action<TutorialStep> OnStepChanged;
        public event Action OnTutorialComplete;

        private void Awake()
        {
            if (_instance != null && _instance != this) { Destroy(gameObject); return; }
            _instance = this;
            DontDestroyOnLoad(gameObject);

            // Load all steps from CSV via CsvLoader
            _allSteps = Utils.CsvLoader.Load<TutorialStep>(TutorialStep.ResourcePath);
            _sequencer.OnStepChanged += Step => OnStepChanged?.Invoke(Step);
            _sequencer.OnComplete += CompleteActiveTutorial;
        }

        private void Start()
        {
            LoadProgress();
        }

        public void LoadProgress()
        {
            if (TutorialApiService.Instance != null && AuthService.Instance != null && !AuthService.Instance.IsGuest)
            {
                TutorialApiService.Instance.FetchProgress(
                    onSuccess: response =>
                    {
                        foreach (var id in response.CompletedTutorialIds)
                        {
                            _completedTutorialIds.Add(id);
                        }
                    },
                    onError: err =>
                    {
                        Debug.LogWarning($"[TutorialManager] Failed to fetch server progress: {err}, falling back to PlayerPrefs");
                        LoadLocalProgress();
                    }
                );
            }
            else
            {
                LoadLocalProgress();
            }
        }

        private void LoadLocalProgress()
        {
            if (_allSteps == null) return;
            foreach (var step in _allSteps)
            {
                int groupId = GetGroupId(step.id);
                if (PlayerPrefs.GetInt("tut_done_" + groupId, 0) == 1)
                {
                    _completedTutorialIds.Add(groupId);
                }
            }
        }

        private int GetGroupId(int id)
        {
            // Group by hundreds digit (e.g. 101-105 is group 100, 201 is 200, 301 is 300)
            return (id / 100) * 100;
        }

        public bool IsGroupCompleted(int id)
        {
            return _completedTutorialIds.Contains(GetGroupId(id));
        }

        public void CheckLobbyTriggers()
        {
            // Lobby scene tutorial triggers. For MVP, we unlock lobby after Stage 3, and no specific lobby tutorials are enforced.
        }

        public void OnBoardReady(int stageId, BoardState board)
        {
            StartCoroutine(EvaluateBoardTriggersCoroutine(stageId, board));
        }

        private IEnumerator EvaluateBoardTriggersCoroutine(int stageId, BoardState board)
        {
            // Delay by a frame to make sure all view gameobjects (HUD, BoardView) are fully instantiated in scene
            yield return new WaitForEndOfFrame();

            if (_allSteps == null || _allSteps.Length == 0) yield break;

            // 1. Stage 1 / Stage 2 Forced/Semi-guided loop onboarding (Phase A)
            if (stageId == 1 && !IsGroupCompleted(100))
            {
                TriggerGroup(100);
                yield break;
            }
            if (stageId == 2 && !IsGroupCompleted(200))
            {
                TriggerGroup(200);
                yield break;
            }

            // 2. Gimmick Contextual Onboarding (Phase B)
            if (board != null)
            {
                bool hasProtector = false;
                bool hasCore = false;
                bool hasObstacle = false;

                for (int r = 0; r < board.Height; r++)
                {
                    for (int c = 0; c < board.Width; c++)
                    {
                        var cell = board.Grid[r, c];
                        if (cell.HasValue)
                        {
                            if (cell.Value.protector_strength > 0) hasProtector = true;
                            if (cell.Value.is_core) hasCore = true;
                            if (cell.Value.cell_type == CellType.Obstacle) hasObstacle = true;
                        }
                    }
                }

                if (hasProtector && !IsGroupCompleted(300))
                {
                    TriggerGroup(300);
                    yield break;
                }
                if (hasCore && !IsGroupCompleted(400))
                {
                    TriggerGroup(400);
                    yield break;
                }
                if (hasObstacle && !IsGroupCompleted(500))
                {
                    TriggerGroup(500);
                    yield break;
                }
            }
        }

        public void CheckFailTriggers(int stageId, int failCount)
        {
            // 3. Fail repeat item hint (Phase C)
            if (failCount >= 3 && !IsGroupCompleted(600))
            {
                // Verify if player has any item (can use inventory)
                bool hasItems = false;
                var progress = PlayerProgressService.Instance;
                if (progress != null)
                {
                    for (int itemId = 1; itemId <= 6; itemId++)
                    {
                        if (progress.GetItemCount(itemId) > 0)
                        {
                            hasItems = true;
                            break;
                        }
                    }
                }

                if (hasItems)
                {
                    TriggerGroup(600);
                }
            }
        }

        private void TriggerGroup(int groupId)
        {
            var steps = new List<TutorialStep>();
            foreach (var step in _allSteps)
            {
                if (GetGroupId(step.id) == groupId)
                {
                    steps.Add(step);
                }
            }
            steps.Sort((a, b) => a.step_index.CompareTo(b.step_index));

            if (steps.Count == 0) return;

            Debug.Log($"[TutorialManager] Triggered Tutorial Group: {groupId}");
            _activeGroupId = groupId;
            _sequencer.Start(steps);

            // Show UIManager overlay
            Core.UIManager.Instance?.ShowOverlay<Core.UI.TutorialOverlay>(overlay =>
            {
                overlay.Init(_sequencer);
            });
        }

        public void NextStep()
        {
            if (_sequencer.IsActive)
            {
                _sequencer.Next();
            }
        }

        private void CompleteActiveTutorial()
        {
            if (_activeGroupId != -1)
            {
                CompleteTutorialGroup(_activeGroupId);
                _activeGroupId = -1;
            }
            OnTutorialComplete?.Invoke();
        }

        public void CompleteTutorialGroup(int groupId)
        {
            _completedTutorialIds.Add(groupId);
            PlayerPrefs.SetInt("tut_done_" + groupId, 1);
            PlayerPrefs.Save();

            if (TutorialApiService.Instance != null && AuthService.Instance != null && !AuthService.Instance.IsGuest)
            {
                TutorialApiService.Instance.CompleteTutorial(groupId,
                    onSuccess: response => Debug.Log($"[TutorialManager] Saved progress for group {groupId} on server"),
                    onError: err => Debug.LogWarning($"[TutorialManager] Server save progress failed for group {groupId}: {err}")
                );
            }

            Core.UIManager.Instance?.CloseOverlay();
        }
    }
}
