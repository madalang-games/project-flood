using System.Collections.Generic;
using Game.InGame.Board;
using ProjectFlood.Contracts.GameTypes;

namespace Game.InGame.Items
{
    // 3x3 blast centered on target (all 9 cells, including center).
    // Void positions and out-of-bounds are skipped.
    public class BombEffect : IItemEffect
    {
        public List<(int row, int col)> GetAffectedCells(BoardState board, int targetRow, int targetCol)
        {
            var cells = new List<(int, int)>();
            for (int dr = -1; dr <= 1; dr++)
            for (int dc = -1; dc <= 1; dc++)
            {
                int r = targetRow + dr;
                int c = targetCol + dc;
                if (r < 0 || r >= board.Height || c < 0 || c >= board.Width) continue;
                var cell = board.Grid[r, c];
                if (cell == null || cell.Value.cell_type == CellType.Void) continue;
                cells.Add((r, c));
            }
            return cells;
        }
    }
}
