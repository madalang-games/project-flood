using Game.InGame.Board;

namespace Game.InGame.Rules
{
    public static class GravitySystem
    {
        // Packs non-null cells downward (toward higher row indices) per column
        public static void Apply(BoardState board)
        {
            for (int c = 0; c < board.Width; c++)
            {
                int writeRow = board.Height - 1;
                for (int r = board.Height - 1; r >= 0; r--)
                {
                    if (board.Grid[r, c] != null)
                        board.Grid[writeRow--, c] = board.Grid[r, c];
                }
                while (writeRow >= 0)
                    board.Grid[writeRow--, c] = null;
            }
        }
    }
}
