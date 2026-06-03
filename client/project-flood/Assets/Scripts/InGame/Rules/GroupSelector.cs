using System.Collections.Generic;
using Game.InGame.Board;

namespace Game.InGame.Rules
{
    public static class GroupSelector
    {
        private static readonly (int dr, int dc)[] Directions = { (-1, 0), (1, 0), (0, -1), (0, 1) };

        public static List<(int row, int col)> FindGroup(BoardState board, int row, int col)
        {
            var result = new List<(int, int)>();
            var cell = board.Grid[row, col];
            if (cell == null) return result;

            int targetColor = cell.Value.color_id;
            var visited = new bool[board.Height, board.Width];
            var queue = new Queue<(int, int)>();
            queue.Enqueue((row, col));
            visited[row, col] = true;

            while (queue.Count > 0)
            {
                var (r, c) = queue.Dequeue();
                result.Add((r, c));
                foreach (var (dr, dc) in Directions)
                {
                    int nr = r + dr, nc = c + dc;
                    if (nr < 0 || nr >= board.Height || nc < 0 || nc >= board.Width) continue;
                    if (visited[nr, nc]) continue;
                    var neighbor = board.Grid[nr, nc];
                    if (neighbor == null || neighbor.Value.color_id != targetColor) continue;
                    visited[nr, nc] = true;
                    queue.Enqueue((nr, nc));
                }
            }
            return result;
        }
    }
}
