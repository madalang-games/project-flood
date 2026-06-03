using System.Collections.Generic;
using Game.InGame.Board;
using ProjectFlood.Data.Generated;
using UnityEngine;

namespace Game.InGame.View
{
    public class BoardView : MonoBehaviour
    {
        [SerializeField] private CellView _cellPrefab;
        [SerializeField] private float _cellSize = 1f;

        private CellView[,] _cellViews;
        private BoardState _board;
        private Color[] _colorPalette;

        public void Build(BoardState board, int[] colorIds)
        {
            _colorPalette = LoadPalette();
            _board = board;
            _cellViews = new CellView[board.Height, board.Width];

            float startX = -(board.Width * _cellSize) / 2f + _cellSize / 2f;
            float startY =  (board.Height * _cellSize) / 2f - _cellSize / 2f;

            for (int r = 0; r < board.Height; r++)
            for (int c = 0; c < board.Width; c++)
            {
                var view = Instantiate(_cellPrefab, transform);
                view.transform.localPosition = new Vector3(
                    startX + c * _cellSize,
                    startY - r * _cellSize,
                    0f);
                _cellViews[r, c] = view;
            }

            Refresh(board);
        }

        public void Refresh(BoardState board)
        {
            for (int r = 0; r < board.Height; r++)
            for (int c = 0; c < board.Width; c++)
            {
                var cell = board.Grid[r, c];
                Color color = cell != null ? GetColor(cell.Value.color_id) : Color.white;
                _cellViews[r, c].SetData(cell, color);
            }
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
    }
}
