using Game.InGame.Board;
using ProjectFlood.Contracts.GameTypes;

namespace Game.InGame.Rules
{
    public static class GravitySystem
    {
        // Packs non-null, non-Void cells downward per column.
        // Void cells are fixed and act as segment boundaries — cells cannot fall through them.
        public static void Apply(BoardState board)
        {
            for (int c = 0; c < board.Width; c++)
            {
                int writeRow = board.Height - 1;
                for (int r = board.Height - 1; r >= 0; r--)
                {
                    var cell = board.Grid[r, c];
                    if (cell?.cell_type == CellType.Void)
                    {
                        while (writeRow > r)
                            board.Grid[writeRow--, c] = null;
                        writeRow = r - 1;
                        continue;
                    }
                    if (cell != null)
                        board.Grid[writeRow--, c] = cell;
                }
                while (writeRow >= 0)
                    board.Grid[writeRow--, c] = null;
            }
        }
    }
}
