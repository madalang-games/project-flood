using System.Collections.Generic;
using Game.InGame.Board;
using ProjectFlood.Contracts.GameTypes;

namespace Game.InGame.Items
{
    public enum ShiftDirection
    {
        Left,
        Right
    }

    // Packs all cells in each row toward the swipe direction.
    // Void positions act as hard boundaries; segment on each side shifts independently.
    public class RowShiftEffect : IItemEffect
    {
        public List<(int row, int col)> GetAffectedCells(BoardState board, int targetRow, int targetCol)
        {
            return new List<(int, int)>(); // RowShift does not use tap targeting
        }

        public void Apply(BoardState board, ShiftDirection direction)
        {
            for (int r = 0; r < board.Height; r++)
            {
                int c = 0;
                while (c < board.Width)
                {
                    // Find next non-Void segment
                    while (c < board.Width && board.Grid[r, c].HasValue && board.Grid[r, c].Value.cell_type == CellType.Void)
                    {
                        c++;
                    }

                    if (c >= board.Width) break;

                    int startCol = c;
                    while (c < board.Width && (!board.Grid[r, c].HasValue || board.Grid[r, c].Value.cell_type != CellType.Void))
                    {
                        c++;
                    }
                    int endCol = c - 1; // Segment range: [startCol, endCol]

                    // Collect valid cells in segment
                    var cells = new List<CellData>();
                    for (int col = startCol; col <= endCol; col++)
                    {
                        if (board.Grid[r, col].HasValue)
                        {
                            cells.Add(board.Grid[r, col].Value);
                        }
                    }

                    // Re-populate segment based on direction
                    if (direction == ShiftDirection.Left)
                    {
                        int cellIdx = 0;
                        for (int col = startCol; col <= endCol; col++)
                        {
                            if (cellIdx < cells.Count)
                            {
                                board.Grid[r, col] = cells[cellIdx++];
                            }
                            else
                            {
                                board.Grid[r, col] = null;
                            }
                        }
                    }
                    else // Right
                    {
                        int cellIdx = cells.Count - 1;
                        for (int col = endCol; col >= startCol; col--)
                        {
                            if (cellIdx >= 0)
                            {
                                board.Grid[r, col] = cells[cellIdx--];
                            }
                            else
                            {
                                board.Grid[r, col] = null;
                            }
                        }
                    }
                }
            }
        }
    }
}
