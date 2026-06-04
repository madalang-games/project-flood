using System;
using System.Collections;
using System.Collections.Generic;
using Game.InGame.Board;
using Game.InGame.Items;
using Game.InGame.Rules;
using Game.InGame.View;
using ProjectFlood.Contracts.GameTypes;
using ProjectFlood.Data.Generated;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.InGame.Controller
{
    public class InGameController : MonoBehaviour
    {
        [SerializeField] private BoardView _boardView;
        [SerializeField] private ItemTrayView _itemTrayView;
        [SerializeField] private bool _isDevMode;

        public event Action<StarResult, int> OnStageEnd;   // (result, remainingTurns)
        public event Action<int> OnTurnConsumed;           // remainingTurns

        private BoardState _board;
        private TurnManager _turnManager;
        private Stage _stage;
        private ItemManager _itemManager;
        private bool _isPlaying;
        private bool _isAnimating;

        public void Init(Stage stage)
        {
            _stage = stage;
            _board = StageLoader.Load(stage);
            _turnManager = new TurnManager(stage.turn_limit);
            _boardView.Build(_board, StageLoader.ParseColorIds(stage.color_ids));

            var inventory = new ItemInventory { IsDevMode = _isDevMode };
            _itemManager = new ItemManager(inventory);
            _itemManager.OnUsePhaseChanged += OnItemUsePhaseChanged;

            if (_itemTrayView != null)
            {
                _itemTrayView.OnSlotTapped += OnItemSlotTapped;
                _itemTrayView.Refresh(_itemManager);
            }

            _isPlaying = true;
            _isAnimating = false;
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
                    HandleItemTap(row, col);
                // Void/null tap during UsePhase: no-op (stay in phase)
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
            var cells = _itemManager.UseItem(_board, row, col);
            if (cells == null || cells.Count == 0) return;
            StartCoroutine(HandleItemSequence(row, col, cells));
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
            OnTurnConsumed?.Invoke(_turnManager.RemainingTurns);

            var result = ClearEvaluator.Evaluate(_board, _stage.star1_ratio, _stage.star2_ratio);

            if (result == StarResult.Star3 || !turnsLeft)
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

        private IEnumerator HandleItemSequence(int originRow, int originCol, List<(int row, int col)> cells)
        {
            _isAnimating = true;

            if (_itemTrayView != null) _itemTrayView.SetLocked(true);

            RemovalSystem.Remove(_board, cells);
            yield return _boardView.PlayRemovalEffects(_board, cells, originRow, originCol);

            var beforeGravity = CloneGrid(_board);
            GravitySystem.Apply(_board);
            yield return _boardView.PlayGravity(beforeGravity, _board);

            // Items do not consume turns
            var result = ClearEvaluator.Evaluate(_board, _stage.star1_ratio, _stage.star2_ratio);
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
            _itemManager.SelectItem(type);
        }

        private void OnItemUsePhaseChanged(ItemType? selected)
        {
            _boardView.SetItemTargetMode(selected.HasValue);
            if (_itemTrayView != null)
                _itemTrayView.Refresh(_itemManager);
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
    }
}
