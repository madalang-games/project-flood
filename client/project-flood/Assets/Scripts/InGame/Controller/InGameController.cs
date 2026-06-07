using System;
using System.Collections;
using System.Collections.Generic;
using Game.InGame.Board;
using Game.InGame.Items;
using Game.InGame.Rules;
using Game.InGame.View;
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
                _itemTrayView.Refresh(_itemManager);
            }

            _isPlaying    = true;
            _isAnimating  = false;
            _continueUsed = false;
        }

        private void OnDestroy()
        {
            if (_itemManager != null)
                _itemManager.OnUsePhaseChanged -= OnItemUsePhaseChanged;
            if (_itemTrayView != null)
                _itemTrayView.OnSlotTapped -= OnItemSlotTapped;
        }

        private void Update()
        {
            if (!_isPlaying) return;
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
            if (tapped == null || tapped.Value.cell_type == CellType.Void) return;

            HandleTap(row, col);
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
            yield return _boardView.PlayBoardRotation(2);
            _board.Rotate180();
            _boardView.CompleteBoardRotation(_board);

            var beforeGravity = CloneGrid(_board);
            GravitySystem.Apply(_board);
            yield return _boardView.PlayGravity(beforeGravity, _board);
            _isAnimating = false;
        }

        private IEnumerator HandleTapSequence(int row, int col, List<(int row, int col)> group)
        {
            _isAnimating = true;

            if (_itemTrayView != null) _itemTrayView.SetLocked(true);

            yield return _boardView.PlayTapFeedback(row, col);
            yield return _boardView.PlayGroupPulse(group, row, col);

            RemovalSystem.Remove(_board, group);
            yield return _boardView.PlayRemovalEffects(_board, group, row, col);

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
                    OnContinueAvailable?.Invoke();
                else
                    OnStageEnd?.Invoke(result, _turnManager.RemainingTurns);
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
                RemovalSystem.Remove(_board, cells);
                yield return _boardView.PlayRemovalEffects(_board, cells, originRow, originCol);

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

            var beforeRowShift = CloneGrid(_board);
            _itemManager.UseRowShift(_board, direction);

            yield return _boardView.PlayRowShift(beforeRowShift, _board, direction);

            // Gravity must run after RowShift slide resolves
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

        private void OnItemSlotTapped(ItemType type)
        {
            if (!_isPlaying || _isAnimating) return;
            _boardView.ClearAllCellSelectedHighlights(); // Clear any visual swap highlights
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
    }
}
