using System;
using System.Collections;
using System.Collections.Generic;
using Game.InGame.Board;
using Game.InGame.Rules;
using Game.InGame.View;
using ProjectFlood.Data.Generated;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.InGame.Controller
{
    public class InGameController : MonoBehaviour
    {
        [SerializeField] private BoardView _boardView;

        public event Action<StarResult, int> OnStageEnd;   // (result, remainingTurns)
        public event Action<int> OnTurnConsumed;           // remainingTurns

        private BoardState _board;
        private TurnManager _turnManager;
        private Stage _stage;
        private bool _isPlaying;
        private bool _isAnimating;

        public void Init(Stage stage)
        {
            _stage = stage;
            _board = StageLoader.Load(stage);
            _turnManager = new TurnManager(stage.turn_limit);
            _boardView.Build(_board, StageLoader.ParseColorIds(stage.color_ids));
            _isPlaying = true;
            _isAnimating = false;
        }

        private void Update()
        {
            if (!_isPlaying) return;
            if (_isAnimating) return;

            Vector2? tapPos = ReadTapPosition();
            if (tapPos == null) return;

            var (row, col) = _boardView.ScreenToCell(tapPos.Value);
            if (row < 0 || col < 0) return;
            if (_board.Grid[row, col] == null) return;

            HandleTap(row, col);
        }

        private void HandleTap(int row, int col)
        {
            var group = GroupSelector.FindGroup(_board, row, col);
            StartCoroutine(HandleTapSequence(row, col, group));
        }

        private IEnumerator HandleTapSequence(int row, int col, List<(int row, int col)> group)
        {
            _isAnimating = true;

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

            _isAnimating = false;
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
