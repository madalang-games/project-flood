using System;
using System.Collections;
using System.Collections.Generic;
using Game.Core;
using Game.InGame.Board;
using Game.InGame.Items;
using Game.InGame.Rules;
using Game.InGame.View;
using Game.OutGame.Lobby;
using Game.Services;
using ProjectFlood.Contracts.GameTypes;
using ProjectFlood.Contracts.Stage;
using ProjectFlood.Data.Generated;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.InGame.Controller
{
    public class InGameController : MonoBehaviour
    {
        [SerializeField] private BoardView _boardView;
        [SerializeField] private ItemTrayView _itemTrayView;
        [SerializeField] private RowShiftOverlayView _rowShiftOverlay;
        [SerializeField] private bool _isDevMode;

        public event Action<StarResult, int> OnStageEnd;     // (result, remainingTurns)
        public event Action<int, int>      OnBoardUpdated; // (remainingTurns, remainingCells)
        public event Action                OnContinueAvailable;

        public float Star1Ratio => _stage?.star1_ratio ?? 0.8f;
        public float Star2Ratio => _stage?.star2_ratio ?? 0.9f;
        public int   TotalTurns => _stage?.turn_limit  ?? 0;
        public int   RemainingCells => CountRemainingBasicCells();

        private BoardState _board;
        private TurnManager _turnManager;
        private Stage _stage;
        private ItemManager _itemManager;
        private bool _isPlaying;
        private bool _isAnimating;
        private bool _continueUsed;
        private Vector2 _dragStartPos;
        private bool _isDragging;

        public void Init(Stage stage, int extraTurns = 0)
        {
            _stage = stage;
            _board = StageLoader.Load(stage);

            SpawnStartingBoosters();

            _turnManager = new TurnManager(stage.turn_limit + extraTurns);
            _boardView.Build(_board, StageLoader.ParseColorIds(stage.color_ids));

            // Dynamic find fallback to recover broken prefab references on scene loads
            if (_itemTrayView == null)
            {
                _itemTrayView = FindObjectOfType<ItemTrayView>();
            }

            if (_rowShiftOverlay == null)
            {
                _rowShiftOverlay = FindObjectOfType<RowShiftOverlayView>(true);
            }

            var inventory = new ItemInventory { IsDevMode = _isDevMode };
            _itemManager = new ItemManager(inventory);
            _itemManager.OnUsePhaseChanged += OnItemUsePhaseChanged;

            if (_itemTrayView != null)
            {
                _itemTrayView.OnSlotTapped += OnItemSlotTapped;
                _itemTrayView.SetItemPrices(BuildItemPrices());
                _itemTrayView.Refresh(_itemManager);
            }

            _isPlaying    = true;
            _isAnimating  = false;
            _continueUsed = false;
            if (Services.Tutorial.TutorialManager.Instance != null)
            {
                Services.Tutorial.TutorialManager.Instance.OnBoardReady(stage.stage_id, _board);
            }
        }

        private void OnDestroy()
        {
            if (_itemManager != null)
                _itemManager.OnUsePhaseChanged -= OnItemUsePhaseChanged;
            if (_itemTrayView != null)
                _itemTrayView.OnSlotTapped -= OnItemSlotTapped;
        }

        private void OnDisable()
        {
            _isAnimating = false;
        }

        private void Update()
        {
            if (!_isPlaying) return;
            if (Services.Tutorial.TutorialManager.Instance != null && Services.Tutorial.TutorialManager.Instance.IsBlocking)
            {
                var step = Services.Tutorial.TutorialManager.Instance.CurrentStep;
                if (step != null && step.target_ui_id.StartsWith("board_cell_"))
                {
                    Vector2? tutTapPos = ReadTapPosition();
                    if (tutTapPos != null)
                    {
                        var (tutRow, tutCol) = _boardView.ScreenToCell(tutTapPos.Value);
                        if (tutRow >= 0 && tutCol >= 0)
                        {
                            if (ParseCellTarget(step.target_ui_id, out int tr, out int tc))
                            {
                                if (!_isAnimating && tutRow == tr && tutCol == tc)
                                {
                                    HandleTap(tutRow, tutCol);
                                    Services.Tutorial.TutorialManager.Instance.NextStep();
                                }
                            }
                        }
                    }
                }
                return;
            }
            if (_isAnimating) return;

            // 1. Check RowShift Swipe Input (Higher priority)
            if (_itemManager.IsInUsePhase && _itemManager.SelectedItem == ItemType.RowShift)
            {
                if (_rowShiftOverlay != null && !_rowShiftOverlay.gameObject.activeSelf)
                {
                    _rowShiftOverlay.Show();
                }

                if (ReadPressStart(out Vector2 startPos))
                {
                    _dragStartPos = startPos;
                    _isDragging = true;
                    if (_rowShiftOverlay != null) _rowShiftOverlay.StartDrag(startPos);
                }
                else if (ReadPressEnd(out Vector2 endPos))
                {
                    if (_isDragging)
                    {
                        _isDragging = false;
                        if (_rowShiftOverlay != null) _rowShiftOverlay.EndDrag();

                        float deltaX = endPos.x - _dragStartPos.x;
                        if (Mathf.Abs(deltaX) >= 50f)
                        {
                            ShiftDirection direction = deltaX > 0 ? ShiftDirection.Right : ShiftDirection.Left;
                            StartCoroutine(HandleRowShiftSequence(direction));
                            return;
                        }
                    }
                }
                else if (_isDragging)
                {
                    Vector2 currentPos = GetCurrentInputPosition();
                    if (_rowShiftOverlay != null) _rowShiftOverlay.Dragging(currentPos);
                }
                return;
            }

            // 2. Normal Tap & Other Items Input
            Vector2? tapPos = ReadTapPosition();
            if (tapPos == null) return;

            var (row, col) = _boardView.ScreenToCell(tapPos.Value);

            if (_itemManager.IsInUsePhase)
            {
                if (row < 0 || col < 0)
                {
                    _itemManager.Cancel();
                    return;
                }
                var cell = _board.Grid[row, col];
                if (cell != null && cell.Value.cell_type != CellType.Void)
                {
                    HandleItemTap(row, col);
                }
                return;
            }

            if (row < 0 || col < 0) return;
            var tapped = _board.Grid[row, col];
            if (tapped == null || tapped.Value.cell_type == CellType.Void || tapped.Value.cell_type == CellType.Obstacle) return;

            if (tapped.Value.cell_type == CellType.Bomb || tapped.Value.cell_type == CellType.HRocket || tapped.Value.cell_type == CellType.ColorSweep)
            {
                HandleSpecialCellTap(row, col, tapped.Value.cell_type);
            }
            else
            {
                HandleTap(row, col);
            }
        }

        public void TriggerRotateBoard()
        {
            if (!_isPlaying || _isAnimating) return;
            _itemManager.Cancel();
            StartCoroutine(RotateBoardSequence());
        }

        private void HandleTap(int row, int col)
        {
            var group = GroupSelector.FindGroup(_board, row, col);
            StartCoroutine(HandleTapSequence(row, col, group));
        }

        private void HandleItemTap(int row, int col)
        {
            var itemType = _itemManager.SelectedItem ?? ItemType.Bomb;
            bool completed;
            var cells = _itemManager.UseItem(_board, row, col, out completed);
            
            // For CellSwap, first tap returns completed = false, cells = null.
            // Highlight the first selected cell if set.
            if (itemType == ItemType.CellSwap)
            {
                if (_itemManager.FirstSelectedCell.HasValue)
                {
                    var first = _itemManager.FirstSelectedCell.Value;
                    _boardView.SetCellSelectedHighlight(first.row, first.col, true);
                }
                else
                {
                    // Reset highlights
                    _boardView.ClearAllCellSelectedHighlights();
                }
            }

            if (!completed || cells == null || cells.Count == 0) return;

            // Clear CellSwap selection highlight on completion
            _boardView.ClearAllCellSelectedHighlights();

            StartCoroutine(HandleItemSequence(row, col, cells, itemType));
        }

        private IEnumerator RotateBoardSequence()
        {
            _isAnimating = true;
            yield return RotateBoardAnimation();
            _isAnimating = false;
        }

        private IEnumerator RotateBoardAnimation()
        {
            SfxEventBus.Play(SfxId.BoardRotated);
            yield return _boardView.PlayBoardRotation(2);
            _board.Rotate180();
            _boardView.CompleteBoardRotation(_board);

            var beforeGravity = CloneGrid(_board);
            GravitySystem.Apply(_board);
            yield return _boardView.PlayGravity(beforeGravity, _board);
        }

        private IEnumerator HandleTapSequence(int row, int col, List<(int row, int col)> group)
        {
            _isAnimating = true;

            if (_itemTrayView != null) _itemTrayView.SetLocked(true);

            yield return _boardView.PlayTapFeedback(row, col);
            yield return _boardView.PlayGroupPulse(group, row, col);

            RemovalSystem.Remove(_board, group);
            SfxEventBus.Play(SfxId.CellGroupRemoved);
            yield return _boardView.PlayRemovalEffects(_board, group, row, col);

            ShiftConveyors();

            var beforeGravity = CloneGrid(_board);
            GravitySystem.Apply(_board);
            yield return _boardView.PlayGravity(beforeGravity, _board);

            bool turnsLeft = _turnManager.Consume();
            var result = ClearEvaluator.Evaluate(_board, _stage.star1_ratio, _stage.star2_ratio);
            OnBoardUpdated?.Invoke(_turnManager.RemainingTurns, CountRemainingBasicCells());

            if (result == StarResult.Star3 || !turnsLeft)
            {
                _isPlaying = false;
                if (!turnsLeft && result == StarResult.Fail && !_continueUsed)
                {
                    SfxEventBus.Play(SfxId.StageFail);
                    OnContinueAvailable?.Invoke();
                }
                else
                {
                    SfxEventBus.Play(result == StarResult.Fail ? SfxId.StageFail : SfxId.StageClear);
                    OnStageEnd?.Invoke(result, _turnManager.RemainingTurns);
                }
            }

            if (_isPlaying && _stage.rotation_interval > 0)
            {
                int movesMade = _turnManager.UsedTurns;
                if (movesMade > 0 && movesMade % _stage.rotation_interval == 0)
                {
                    yield return RotateBoardAnimation();
                }
            }

            if (_itemTrayView != null)
            {
                _itemTrayView.SetLocked(false);
                _itemTrayView.Refresh(_itemManager);
            }

            _isAnimating = false;
        }

        private IEnumerator HandleItemSequence(int originRow, int originCol, List<(int row, int col)> cells, ItemType itemType)
        {
            _isAnimating = true;

            if (_itemTrayView != null) _itemTrayView.SetLocked(true);

            if (!_isDevMode && Game.Services.InventoryApiService.Instance != null)
            {
                int itemId = itemType switch
                {
                    ItemType.Bomb => 2,
                    ItemType.HRocket => 3,
                    ItemType.ColorSweep => 4,
                    ItemType.CellSwap => 6,
                    _ => 0
                };
                if (itemId > 0)
                {
                    Game.Services.InventoryApiService.Instance.SpendItem(itemId, 1, "use_in_game",
                        onSuccess: snap => Debug.Log($"[InGameController] spent item {itemId} on server"),
                        onError: err => Debug.LogWarning($"[InGameController] failed to spend item {itemId}: {err}"));
                }
            }

            SfxEventBus.Play(SfxId.ItemUsed);

            if (itemType == ItemType.CellSwap)
            {
                // Play swap animation, NO gravity applied for CellSwap per rules
                var first = cells[0];
                var second = cells[1];
                yield return _boardView.PlayCellSwap(first.row, first.col, second.row, second.col);
                _boardView.Refresh(_board);
            }
            else
            {
                bool allowObstacle = (itemType == ItemType.Bomb || itemType == ItemType.HRocket);
                RemovalSystem.Remove(_board, cells, allowObstacle);
                yield return _boardView.PlayRemovalEffects(_board, cells, originRow, originCol);

                ShiftConveyors();

                var beforeGravity = CloneGrid(_board);
                GravitySystem.Apply(_board);
                yield return _boardView.PlayGravity(beforeGravity, _board);
            }

            // Items do not consume turns
            var result = ClearEvaluator.Evaluate(_board, _stage.star1_ratio, _stage.star2_ratio);
            OnBoardUpdated?.Invoke(_turnManager.RemainingTurns, CountRemainingBasicCells());
            if (result == StarResult.Star3)
            {
                _isPlaying = false;
                SfxEventBus.Play(SfxId.StageClear);
                OnStageEnd?.Invoke(result, _turnManager.RemainingTurns);
            }

            if (_itemTrayView != null)
            {
                _itemTrayView.SetLocked(false);
                _itemTrayView.Refresh(_itemManager);
            }

            _isAnimating = false;
        }

        private IEnumerator HandleRowShiftSequence(ShiftDirection direction)
        {
            _isAnimating = true;

            if (_itemTrayView != null) _itemTrayView.SetLocked(true);

            if (!_isDevMode && Game.Services.InventoryApiService.Instance != null)
            {
                Game.Services.InventoryApiService.Instance.SpendItem(5, 1, "use_in_game",
                    onSuccess: snap => Debug.Log("[InGameController] spent row_shift on server"),
                    onError: err => Debug.LogWarning($"[InGameController] failed to spend row_shift: {err}"));
            }

            var beforeRowShift = CloneGrid(_board);
            _itemManager.UseRowShift(_board, direction);

            yield return _boardView.PlayRowShift(beforeRowShift, _board, direction);

            // Gravity must run after RowShift slide resolves
            ShiftConveyors();

            var beforeGravity = CloneGrid(_board);
            GravitySystem.Apply(_board);
            yield return _boardView.PlayGravity(beforeGravity, _board);

            // Items do not consume turns
            var result = ClearEvaluator.Evaluate(_board, _stage.star1_ratio, _stage.star2_ratio);
            OnBoardUpdated?.Invoke(_turnManager.RemainingTurns, CountRemainingBasicCells());
            if (result == StarResult.Star3)
            {
                _isPlaying = false;
                OnStageEnd?.Invoke(result, _turnManager.RemainingTurns);
            }

            if (_itemTrayView != null)
            {
                _itemTrayView.SetLocked(false);
                _itemTrayView.Refresh(_itemManager);
            }

            _isAnimating = false;
        }

        private static readonly Dictionary<ItemType, int> ItemTypeToId = new()
        {
            { ItemType.Bomb,       2 },
            { ItemType.HRocket,    3 },
            { ItemType.ColorSweep, 4 },
            { ItemType.RowShift,   5 },
            { ItemType.CellSwap,   6 },
        };

        private Dictionary<ItemType, int> BuildItemPrices()
        {
            var prices = new Dictionary<ItemType, int>();
            foreach (var kv in ItemTypeToId)
            {
                var data = Services.ItemDataService.Instance?.GetItem(kv.Value);
                if (data != null) prices[kv.Key] = data.price;
            }
            return prices;
        }

        private void OnItemSlotTapped(ItemType type)
        {
            if (!_isPlaying || _isAnimating) return;
            _boardView.ClearAllCellSelectedHighlights();

            if (!_isDevMode && _itemManager.GetCount(type) <= 0)
            {
                if (!ItemTypeToId.TryGetValue(type, out int itemId)) return;

                var itemData = Services.ItemDataService.Instance?.GetItem(itemId);
                var price = itemData?.price ?? 100;
                var ownedGold = Services.PlayerProgressService.Instance?.Gold ?? 0;
                var icon = _itemTrayView?.GetSlotSprite(type);
                var itemName = itemData != null
                    ? LocalizationService.Instance.Get(itemData.name_key)
                    : type.ToString();
                var itemDesc = itemData != null
                    ? LocalizationService.Instance.Get(itemData.desc_key)
                    : string.Empty;

                Core.UIManager.Instance?.ShowPopup<View.ItemBuyConfirmPopupView>(popup =>
                {
                    popup.Init(icon, itemName, itemDesc, price, ownedGold, () =>
                    {
                        Core.UIManager.Instance?.ShowLoading();
                        Services.InventoryApiService.Instance.BuyItem(itemId,
                            onSuccess: _ =>
                            {
                                Core.UIManager.Instance?.HideLoading();
                                Core.UIManager.Instance?.ShowToast(LocalizationService.Instance.Get("toast.booster_purchased"), Core.UI.ToastType.Success);
                                _itemTrayView?.Refresh(_itemManager);
                                _itemManager.SelectItem(type);
                            },
                            onError: err =>
                            {
                                Core.UIManager.Instance?.HideLoading();
                                Core.UIManager.Instance?.ShowToast($"Purchase failed: {err}", Core.UI.ToastType.Warning);
                            });
                    });
                });
                return;
            }

            _itemManager.SelectItem(type);
        }

        private void OnItemUsePhaseChanged(ItemType? selected)
        {
            _boardView.SetItemTargetMode(selected.HasValue);
            if (!selected.HasValue)
            {
                _boardView.ClearAllCellSelectedHighlights();
            }
            if (_itemTrayView != null)
                _itemTrayView.Refresh(_itemManager);

            if (_rowShiftOverlay != null)
            {
                if (selected == ItemType.RowShift)
                    _rowShiftOverlay.Show();
                else
                    _rowShiftOverlay.Hide();
            }
        }

        public float ComputeRatioPublic() => ComputeRatio();

        public StageAttemptClearRequest BuildClearRequest(string clientRequestId)
        {
            return new StageAttemptClearRequest
            {
                ClientRequestId = clientRequestId,
                RulesetVersion = _stage?.ruleset_version ?? 0,
                TurnsUsed = _stage == null || _turnManager == null ? 0 : _stage.turn_limit - _turnManager.RemainingTurns,
                RemainingBasicCells = CountRemainingBasicCells(),
                CoreRemaining = HasCoreRemaining(),
            };
        }

        public void Continue(int extraTurns)
        {
            _continueUsed = true;
            _turnManager.AddTurns(extraTurns);
            _isPlaying = true;
            OnBoardUpdated?.Invoke(_turnManager.RemainingTurns, CountRemainingBasicCells());
        }

        public void Forfeit()
        {
            _isPlaying = false;
            OnStageEnd?.Invoke(StarResult.Fail, 0);
        }

        private float ComputeRatio()
        {
            if (_board == null || _board.InitialValidCells == 0) return 0f;
            int remaining = CountRemainingBasicCells();
            return (_board.InitialValidCells - remaining) / (float)_board.InitialValidCells;
        }

        private int CountRemainingBasicCells()
        {
            if (_board == null) return 0;
            int remaining = 0;
            for (int r = 0; r < _board.Height; r++)
            for (int c = 0; c < _board.Width;  c++)
            {
                var cell = _board.Grid[r, c];
                if (cell == null) continue;
                var t = cell.Value.cell_type;
                if (t == ProjectFlood.Contracts.GameTypes.CellType.Obstacle ||
                    t == ProjectFlood.Contracts.GameTypes.CellType.Void) continue;
                remaining++;
            }
            return remaining;
        }

        private bool HasCoreRemaining()
        {
            if (_board == null || !_board.HasCore) return false;
            for (int r = 0; r < _board.Height; r++)
            for (int c = 0; c < _board.Width; c++)
            {
                var cell = _board.Grid[r, c];
                if (cell.HasValue && cell.Value.is_core)
                    return true;
            }
            return false;
        }

        private static CellData?[,] CloneGrid(BoardState board)
        {
            var clone = new CellData?[board.Height, board.Width];
            for (int r = 0; r < board.Height; r++)
            for (int c = 0; c < board.Width; c++)
                clone[r, c] = board.Grid[r, c];
            return clone;
        }

        private static Vector2? ReadTapPosition()
        {
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                return Mouse.current.position.ReadValue();

            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
                return Touchscreen.current.primaryTouch.position.ReadValue();

            return null;
        }

        private static Vector2 GetCurrentInputPosition()
        {
            if (Mouse.current != null)
                return Mouse.current.position.ReadValue();
            if (Touchscreen.current != null)
                return Touchscreen.current.primaryTouch.position.ReadValue();
            return Vector2.zero;
        }

        private static bool ReadPressStart(out Vector2 pos)
        {
            pos = Vector2.zero;
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                pos = Mouse.current.position.ReadValue();
                return true;
            }
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            {
                pos = Touchscreen.current.primaryTouch.position.ReadValue();
                return true;
            }
            return false;
        }

        private static bool ReadPressEnd(out Vector2 pos)
        {
            pos = Vector2.zero;
            if (Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame)
            {
                pos = Mouse.current.position.ReadValue();
                return true;
            }
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasReleasedThisFrame)
            {
                pos = Touchscreen.current.primaryTouch.position.ReadValue();
                return true;
            }
            return false;
        }

        private static bool ParseCellTarget(string targetId, out int row, out int col)
        {
            row = -1;
            col = -1;
            try
            {
                int rStart = targetId.IndexOf('[');
                int rEnd = targetId.IndexOf(']');
                int cStart = targetId.IndexOf('[', rEnd + 1);
                int cEnd = targetId.IndexOf(']', cStart + 1);
                
                if (rStart >= 0 && rEnd > rStart && cStart > rEnd && cEnd > cStart)
                {
                    string rStr = targetId.Substring(rStart + 1, rEnd - rStart - 1);
                    string cStr = targetId.Substring(cStart + 1, cEnd - cStart - 1);
                    if (int.TryParse(rStr, out row) && int.TryParse(cStr, out col))
                    {
                        // CSV uses 1-based indexing; convert to 0-based for board array
                        row -= 1;
                        col -= 1;
                        return true;
                    }
                }
            }
            catch {}
            return false;
        }
        private void SpawnStartingBoosters()
        {
            if (_board == null) return;

            var validCoords = new List<(int r, int c)>();
            for (int r = 0; r < _board.Height; r++)
            {
                for (int c = 0; c < _board.Width; c++)
                {
                    var cell = _board.Grid[r, c];
                    if (cell.HasValue && cell.Value.cell_type == CellType.Basic && !cell.Value.is_core && cell.Value.protector_strength == 0)
                    {
                        validCoords.Add((r, c));
                    }
                }
            }

            var rnd = new System.Random();
            for (int i = validCoords.Count - 1; i > 0; i--)
            {
                int j = rnd.Next(i + 1);
                var temp = validCoords[i];
                validCoords[i] = validCoords[j];
                validCoords[j] = temp;
            }

            var boostersToSpawn = new List<CellType>();

            if (ScrollStateCache.UseStartingBomb)
            {
                boostersToSpawn.Add(CellType.Bomb);
                ScrollStateCache.UseStartingBomb = false;
            }
            if (ScrollStateCache.UseStartingHRocket)
            {
                boostersToSpawn.Add(CellType.HRocket);
                ScrollStateCache.UseStartingHRocket = false;
            }

            int streak = ScrollStateCache.CurrentWinStreak;
            if (streak >= 3)
            {
                boostersToSpawn.Add(CellType.HRocket);
                boostersToSpawn.Add(CellType.Bomb);
                boostersToSpawn.Add(CellType.ColorSweep);
            }
            else if (streak == 2)
            {
                boostersToSpawn.Add(CellType.HRocket);
                boostersToSpawn.Add(CellType.Bomb);
            }
            else if (streak == 1)
            {
                boostersToSpawn.Add(CellType.HRocket);
            }

            int spawnCount = Mathf.Min(boostersToSpawn.Count, validCoords.Count);
            for (int i = 0; i < spawnCount; i++)
            {
                var (r, c) = validCoords[i];
                var oldCell = _board.Grid[r, c].Value;
                _board.Grid[r, c] = new CellData
                {
                    color_id = oldCell.color_id,
                    cell_type = boostersToSpawn[i],
                    protector_strength = 0,
                    is_core = false
                };
                Debug.Log($"[InGameController] Spawned starting booster {boostersToSpawn[i]} at ({r}, {c})");
            }
        }

        private void HandleSpecialCellTap(int row, int col, CellType type)
        {
            ItemType itemType = type switch
            {
                CellType.Bomb => ItemType.Bomb,
                CellType.HRocket => ItemType.HRocket,
                CellType.ColorSweep => ItemType.ColorSweep,
                _ => ItemType.Bomb
            };

            IItemEffect effect = itemType switch
            {
                ItemType.Bomb => new BombEffect(),
                ItemType.HRocket => new HRocketEffect(),
                ItemType.ColorSweep => new ColorSweepEffect(),
                _ => null
            };

            if (effect == null) return;

            var cells = effect.GetAffectedCells(_board, row, col);
            if (cells == null || cells.Count == 0) return;

            StartCoroutine(HandleSpecialCellSequence(row, col, cells, itemType));
        }

        private IEnumerator HandleSpecialCellSequence(int originRow, int originCol, List<(int row, int col)> cells, ItemType itemType)
        {
            _isAnimating = true;

            if (_itemTrayView != null) _itemTrayView.SetLocked(true);

            bool allowObstacle = (itemType == ItemType.Bomb || itemType == ItemType.HRocket);
            RemovalSystem.Remove(_board, cells, allowObstacle);
            SfxEventBus.Play(SfxId.CellGroupRemoved);
            yield return _boardView.PlayRemovalEffects(_board, cells, originRow, originCol);

            ShiftConveyors();

            var beforeGravity = CloneGrid(_board);
            GravitySystem.Apply(_board);
            yield return _boardView.PlayGravity(beforeGravity, _board);

            var result = ClearEvaluator.Evaluate(_board, _stage.star1_ratio, _stage.star2_ratio);
            OnBoardUpdated?.Invoke(_turnManager.RemainingTurns, CountRemainingBasicCells());
            if (result == StarResult.Star3)
            {
                _isPlaying = false;
                SfxEventBus.Play(SfxId.StageClear);
                OnStageEnd?.Invoke(result, _turnManager.RemainingTurns);
            }

            if (_itemTrayView != null)
            {
                _itemTrayView.SetLocked(false);
                _itemTrayView.Refresh(_itemManager);
            }

            _isAnimating = false;
        }

        private void ShiftConveyors()
        {
            if (_board == null || _board.ConveyorPaths == null) return;

            foreach (var path in _board.ConveyorPaths)
            {
                if (path == null || path.Count < 2) continue;

                int lastIdx = path.Count - 1;
                var lastCell = _board.Grid[path[lastIdx].r, path[lastIdx].c];

                for (int i = lastIdx; i > 0; i--)
                {
                    var to = path[i];
                    var from = path[i - 1];
                    _board.Grid[to.r, to.c] = _board.Grid[from.r, from.c];
                }

                var first = path[0];
                _board.Grid[first.r, first.c] = lastCell;
            }
        }
    }
}
