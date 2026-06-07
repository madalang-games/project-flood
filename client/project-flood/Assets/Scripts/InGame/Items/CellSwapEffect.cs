using System.Collections.Generic;
using Game.InGame.Board;
using ProjectFlood.Contracts.GameTypes;

namespace Game.InGame.Items
{
    // Checks if the target cell is valid for swapping (non-null, non-Void).
    // The actual swap logic is handled by ItemManager or InGameController.
    public class CellSwapEffect : IItemEffect
    {
        public List<(int row, int col)> GetAffectedCells(BoardState board, int targetRow, int targetCol)
        {
            var cells = new List<(int, int)>();
            if (targetRow < 0 || targetRow >= board.Height || targetCol < 0 || targetCol >= board.Width)
                return cells;

            var cell = board.Grid[targetRow, targetCol];
            if (cell != null && cell.Value.cell_type != CellType.Void)
            {
                cells.Add((targetRow, targetCol));
            }
            return cells;
        }
    }
}
