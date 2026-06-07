using System.Collections.Generic;
using Game.InGame.Board;
using ProjectFlood.Contracts.GameTypes;

namespace Game.InGame.Items
{
    // Clears all cells on the board matching the color of the tapped cell.
    // Obstacles are excluded. Protector layers are stripped by one.
    public class ColorSweepEffect : IItemEffect
    {
        public List<(int row, int col)> GetAffectedCells(BoardState board, int targetRow, int targetCol)
        {
            var cells = new List<(int, int)>();
            var targetCell = board.Grid[targetRow, targetCol];
            if (targetCell == null || targetCell.Value.cell_type == CellType.Void || targetCell.Value.cell_type == CellType.Obstacle)
                return cells;

            int targetColor = targetCell.Value.color_id;

            for (int r = 0; r < board.Height; r++)
            for (int c = 0; c < board.Width; c++)
            {
                var cell = board.Grid[r, c];
                if (cell == null || cell.Value.cell_type == CellType.Void || cell.Value.cell_type == CellType.Obstacle)
                    continue;

                if (cell.Value.color_id == targetColor)
                {
                    cells.Add((r, c));
                }
            }

            return cells;
        }
    }
}
