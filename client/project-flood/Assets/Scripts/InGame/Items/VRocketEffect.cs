using System.Collections.Generic;
using Game.InGame.Board;
using ProjectFlood.Contracts.GameTypes;

namespace Game.InGame.Items
{
    // Sweeps target column top -> bottom.
    // Void positions are skipped (rocket continues past).
    // Obstacle: destroyed, rocket stops immediately after.
    public class VRocketEffect : IItemEffect
    {
        public List<(int row, int col)> GetAffectedCells(BoardState board, int targetRow, int targetCol)
        {
            var cells = new List<(int, int)>();
            for (int r = 0; r < board.Height; r++)
            {
                var cell = board.Grid[r, targetCol];
                if (cell == null || cell.Value.cell_type == CellType.Void) continue;
                cells.Add((r, targetCol));
                if (cell.Value.cell_type == CellType.Obstacle) break;
            }
            return cells;
        }
    }
}
