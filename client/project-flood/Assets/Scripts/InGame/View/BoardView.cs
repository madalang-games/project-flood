using System.Collections.Generic;
using System.Collections;
using Game.InGame.Board;
using ProjectFlood.Contracts.GameTypes;
using ProjectFlood.Data.Generated;
using UnityEngine;

namespace Game.InGame.View
{
    public class BoardView : MonoBehaviour
    {
        [SerializeField] private CellView _cellPrefab;
        [SerializeField] private BoardBackground _background;
        [Range(0f, 1f)]
        [SerializeField] private float _boardScreenRatio = 0.9f;

        private float _cellSize;
        [SerializeField] private float _tapFeedbackDuration = 0.13f;
        [SerializeField] private float _groupPulseDuration = 0.12f;
        [SerializeField] private float _removeDuration = 0.24f;
        [SerializeField] private float _protectorHitDuration = 0.18f;
        [SerializeField] private float _dropDuration = 0.26f;
        [SerializeField] private float _staggerDelay = 0.025f;
        [SerializeField] private int _burstCount = 7;
        [SerializeField] private float _rotateDuration = 0.42f;
        [SerializeField] private float _rotateScalePulse = 0.035f;

        private CellView[,] _cellViews;
        private Vector3[,] _cellPositions;
        private BoardState _board;
        private Color[] _colorPalette;
        private Vector3 _baseBoardScale;

        private void Awake()
        {
            _baseBoardScale = transform.localScale;
        }

        public void Build(BoardState board, int[] colorIds)
        {
            _colorPalette = LoadPalette();
            _board = board;
            _cellViews = new CellView[board.Height, board.Width];
            _cellPositions = new Vector3[board.Height, board.Width];
            _cellSize = ComputeCellSize(board.Width, board.Height);
            AlignBackground();

            float startX = -(board.Width * _cellSize) / 2f + _cellSize / 2f;
            float startY =  (board.Height * _cellSize) / 2f - _cellSize / 2f;

            for (int r = 0; r < board.Height; r++)
            for (int c = 0; c < board.Width; c++)
            {
                var view = Instantiate(_cellPrefab, transform);
                view.Init(_cellSize);
                var position = new Vector3(
                    startX + c * _cellSize,
                    startY - r * _cellSize,
                    0f);
                view.transform.localPosition = position;
                _cellViews[r, c] = view;
                _cellPositions[r, c] = position;
            }

            if (_background != null)
                _background.Build(board.Width, board.Height, _cellSize, _cellPositions);

            Refresh(board);
        }

        public void Refresh(BoardState board)
        {
            _board = board;
            var showSocket = _background != null ? new bool[board.Height, board.Width] : null;

            for (int r = 0; r < board.Height; r++)
            for (int c = 0; c < board.Width; c++)
            {
                var cell = board.Grid[r, c];
                Color color = cell != null ? GetColor(cell.Value.color_id) : Color.white;
                _cellViews[r, c].transform.localPosition = _cellPositions[r, c];
                _cellViews[r, c].transform.localRotation = Quaternion.identity;
                _cellViews[r, c].SetData(cell, color);

                if (showSocket != null)
                    showSocket[r, c] = cell == null;
            }

            if (_background != null && showSocket != null)
                _background.Refresh(board.Width, board.Height, showSocket);
        }

        public IEnumerator PlayTapFeedback(int row, int col)
        {
            if (!IsValidCell(row, col)) yield break;
            yield return _cellViews[row, col].PlayTapFeedback(_tapFeedbackDuration);
        }

        public IEnumerator PlayGroupPulse(IReadOnlyList<(int row, int col)> group, int originRow, int originCol)
        {
            float maxDelay = 0f;
            foreach (var (row, col) in group)
            {
                if (!IsValidCell(row, col)) continue;
                float delay = Manhattan(row, col, originRow, originCol) * _staggerDelay;
                maxDelay = Mathf.Max(maxDelay, delay);
                StartCoroutine(_cellViews[row, col].PlayGroupPulse(delay, _groupPulseDuration));
            }

            yield return new WaitForSeconds(maxDelay + _groupPulseDuration);
        }

        public IEnumerator PlayRemovalEffects(BoardState boardAfterRemoval, IReadOnlyList<(int row, int col)> group, int originRow, int originCol)
        {
            float maxDelay = 0f;
            foreach (var (row, col) in group)
            {
                if (!IsValidCell(row, col)) continue;
                float delay = Manhattan(row, col, originRow, originCol) * _staggerDelay;
                maxDelay = Mathf.Max(maxDelay, delay);

                bool removed = boardAfterRemoval.Grid[row, col] == null;
                IEnumerator effect = removed
                    ? _cellViews[row, col].PlayRemove(_removeDuration, _burstCount)
                    : _cellViews[row, col].PlayProtectorHit(_protectorHitDuration);
                _cellViews[row, col].StartCoroutine(PlayDelayed(effect, delay));
            }

            yield return new WaitForSeconds(maxDelay + Mathf.Max(_removeDuration, _protectorHitDuration));
            Refresh(boardAfterRemoval);
        }

        public IEnumerator PlayGravity(CellData?[,] beforeGravity, BoardState boardAfterGravity)
        {
            Refresh(boardAfterGravity);

            float maxDelay = 0f;
            for (int c = 0; c < boardAfterGravity.Width; c++)
            {
                var sourceRows = new List<int>();
                for (int r = boardAfterGravity.Height - 1; r >= 0; r--)
                {
                    if (beforeGravity[r, c] != null && beforeGravity[r, c].Value.cell_type != CellType.Void)
                        sourceRows.Add(r);
                }

                int sourceIndex = 0;
                for (int r = boardAfterGravity.Height - 1; r >= 0; r--)
                {
                    if (boardAfterGravity.Grid[r, c] == null) continue;
                    if (sourceIndex >= sourceRows.Count) break;

                    int sourceRow = sourceRows[sourceIndex++];
                    if (sourceRow == r) continue;

                    float distance = Mathf.Abs(r - sourceRow);
                    float delay = c * (_staggerDelay * 0.45f);
                    float duration = _dropDuration + distance * 0.035f;
                    maxDelay = Mathf.Max(maxDelay, delay + duration + 0.11f);
                    StartCoroutine(_cellViews[r, c].PlayDrop(_cellPositions[sourceRow, c], _cellPositions[r, c], delay, duration));
                }
            }

            if (maxDelay > 0f)
                yield return new WaitForSeconds(maxDelay);
        }

        public IEnumerator PlayBoardRotation(int quarterTurns)
        {
            if (quarterTurns == 0) yield break;

            Quaternion from = transform.localRotation;
            Quaternion to = from * Quaternion.Euler(0f, 0f, -90f * quarterTurns);
            Vector3 baseScale = _baseBoardScale;
            Vector3 peakScale = baseScale * (1f + _rotateScalePulse);

            for (float t = 0f; t < _rotateDuration; t += Time.deltaTime)
            {
                float p = Mathf.Clamp01(t / _rotateDuration);
                float eased = EaseInOutBack(p);
                float scaleWave = Mathf.Sin(p * Mathf.PI);

                transform.localRotation = Quaternion.LerpUnclamped(from, to, eased);
                transform.localScale = Vector3.LerpUnclamped(baseScale, peakScale, scaleWave);
                yield return null;
            }

            transform.localRotation = to;
            transform.localScale = baseScale;
        }

        public void CompleteBoardRotation(BoardState board)
        {
            transform.localRotation = Quaternion.identity;
            transform.localScale = _baseBoardScale;
            Refresh(board);
        }

        public (int row, int col) ScreenToCell(Vector2 screenPos)
        {
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(screenPos);
            Vector3 local = transform.InverseTransformPoint(worldPos);

            float offsetX = local.x + (_board.Width * _cellSize) / 2f;
            float offsetY = -local.y + (_board.Height * _cellSize) / 2f;

            int col = Mathf.FloorToInt(offsetX / _cellSize);
            int row = Mathf.FloorToInt(offsetY / _cellSize);

            if (row < 0 || row >= _board.Height || col < 0 || col >= _board.Width)
                return (-1, -1);
            return (row, col);
        }

        private float ComputeCellSize(int boardWidth, int boardHeight)
        {
            var cam = Camera.main;
            float viewH = cam.orthographicSize * 2f;
            float viewW = viewH * ((float)Screen.width / Screen.height);
            float fitW = viewW * _boardScreenRatio / boardWidth;
            float fitH = viewH * _boardScreenRatio / boardHeight;
            return Mathf.Min(fitW, fitH);
        }

        private Color GetColor(int colorId)
        {
            if (_colorPalette == null || colorId < 0 || colorId >= _colorPalette.Length)
                return Color.magenta;
            return _colorPalette[colorId];
        }

        private static Color[] LoadPalette()
        {
            var asset = Resources.Load<TextAsset>(ColorPalette.ResourcePath);
            if (asset == null)
            {
                Debug.LogError($"[BoardView] ColorPalette not found: {ColorPalette.ResourcePath}");
                return System.Array.Empty<Color>();
            }

            var map = new Dictionary<int, Color>();
            var lines = asset.text.Split('\n');
            for (int i = 1; i < lines.Length; i++) // skip header row
            {
                var line = lines[i].Trim();
                if (string.IsNullOrEmpty(line)) continue;
                var cols = line.Split(',');
                if (cols.Length < 4) continue;
                int id = int.Parse(cols[0]);
                float r = int.Parse(cols[1]) / 255f;
                float g = int.Parse(cols[2]) / 255f;
                float b = int.Parse(cols[3]) / 255f;
                map[id] = new Color(r, g, b);
            }

            int maxId = 0;
            foreach (var k in map.Keys) if (k > maxId) maxId = k;
            var palette = new Color[maxId + 1];
            foreach (var kv in map) palette[kv.Key] = kv.Value;
            return palette;
        }

        private bool IsValidCell(int row, int col)
        {
            return _cellViews != null
                && row >= 0
                && row < _cellViews.GetLength(0)
                && col >= 0
                && col < _cellViews.GetLength(1);
        }

        private static IEnumerator PlayDelayed(IEnumerator routine, float delay)
        {
            if (delay > 0f) yield return new WaitForSeconds(delay);
            yield return routine;
        }

        private static int Manhattan(int row, int col, int originRow, int originCol)
        {
            return Mathf.Abs(row - originRow) + Mathf.Abs(col - originCol);
        }

        private void AlignBackground()
        {
            if (_background == null) return;
            if (_background.transform == transform) return;

            _background.transform.SetParent(transform, false);
            _background.transform.localPosition = Vector3.zero;
            _background.transform.localRotation = Quaternion.identity;
            _background.transform.localScale = Vector3.one;
        }

        private static float EaseOutBack(float t)
        {
            t = Mathf.Clamp01(t);
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }

        private static float EaseInOutBack(float t)
        {
            t = Mathf.Clamp01(t);
            const float c1 = 1.70158f;
            const float c2 = c1 * 1.525f;
            return t < 0.5f
                ? (Mathf.Pow(2f * t, 2f) * ((c2 + 1f) * 2f * t - c2)) / 2f
                : (Mathf.Pow(2f * t - 2f, 2f) * ((c2 + 1f) * (t * 2f - 2f) + c2) + 2f) / 2f;
        }
    }
}
