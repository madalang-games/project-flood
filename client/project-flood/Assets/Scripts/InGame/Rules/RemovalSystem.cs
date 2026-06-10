using System.Collections.Generic;
using Game.InGame.Board;
using ProjectFlood.Contracts.GameTypes;

namespace Game.InGame.Rules
{
    public static class RemovalSystem
    {
        public static void Remove(BoardState board, List<(int row, int col)> group, bool allowObstacleRemoval = false)
        {
            foreach (var (r, c) in group)
            {
                var cell = board.Grid[r, c];
                if (cell == null) continue;
                if (cell.Value.cell_type == CellType.Obstacle && !allowObstacleRemoval)
                    continue;

                var mutable = cell.Value;
                if (ProtectorSystem.DirectHit(ref mutable))
                    board.Grid[r, c] = null;
                else
                    board.Grid[r, c] = mutable;
            }
        }
    }
}
