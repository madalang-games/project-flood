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
            int h = board.Height;
            int w = board.Width;

            for (int r = h - 1; r >= 0; r--)
            {
                for (int c = 0; c < w; c++)
                {
                    var cell = board.Grid[r, c];
                    if (cell.HasValue && cell.Value.cell_type == CellType.Void) continue;

                    if (cell == null)
                    {
                        var source = FindGravitySource(board, r, c);
                        if (source.HasValue)
                        {
                            var (sr, sc) = source.Value;
                            board.Grid[r, c] = board.Grid[sr, sc];
                            board.Grid[sr, sc] = null;
                        }
                    }
                }
            }
        }

        private static (int r, int c)? FindGravitySource(BoardState board, int r, int c)
        {
            var currR = r;
            var currC = c;
            int maxSteps = board.Height * board.Width;

            for (int step = 0; step < maxSteps; step++)
            {
                (int nextR, int nextC) next;
                if (board.OutletToInlet.TryGetValue((currR, currC), out var inlet))
                {
                    next = inlet;
                }
                else
                {
                    next = (currR - 1, currC);
                }

                if (next.nextR < 0 || next.nextR >= board.Height || next.nextC < 0 || next.nextC >= board.Width)
                {
                    return null;
                }

                var cell = board.Grid[next.nextR, next.nextC];
                if (cell.HasValue)
                {
                    if (cell.Value.cell_type == CellType.Void)
                    {
                        return null;
                    }
                    return next;
                }

                currR = next.nextR;
                currC = next.nextC;
            }

            return null;
        }
    }
}
