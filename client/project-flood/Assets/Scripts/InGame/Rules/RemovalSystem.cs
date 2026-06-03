using System.Collections.Generic;
using Game.InGame.Board;

namespace Game.InGame.Rules
{
    public static class RemovalSystem
    {
        public static void Remove(BoardState board, List<(int row, int col)> group)
        {
            foreach (var (r, c) in group)
            {
                var cell = board.Grid[r, c];
                if (cell == null) continue;
                var mutable = cell.Value;
                if (ProtectorSystem.DirectHit(ref mutable))
                    board.Grid[r, c] = null;
                else
                    board.Grid[r, c] = mutable;
            }
        }
    }
}
