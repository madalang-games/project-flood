using System.Collections.Generic;
using Game.InGame.Board;
using ProjectFlood.Contracts.GameTypes;

namespace Game.InGame.Items
{
    // Sweeps target row left -> right.
    // Void positions are skipped (rocket continues past).
    // Obstacle: destroyed, rocket stops immediately after.
    public class HRocketEffect : IItemEffect
    {
        public List<(int row, int col)> GetAffectedCells(BoardState board, int targetRow, int targetCol)
        {
            var cells = new List<(int, int)>();
            for (int c = 0; c < board.Width; c++)
            {
                var cell = board.Grid[targetRow, c];
                if (cell == null || cell.Value.cell_type == CellType.Void) continue;
                cells.Add((targetRow, c));
                if (cell.Value.cell_type == CellType.Obstacle) break;
            }
            return cells;
        }
    }
}
