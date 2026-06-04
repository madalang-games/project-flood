using Game.InGame.Board;
using ProjectFlood.Contracts.GameTypes;

namespace Game.InGame.Rules
{
    public static class ClearEvaluator
    {
        public static StarResult Evaluate(BoardState board, float star1Ratio, float star2Ratio)
        {
            int remaining = 0;
            bool coreCleared = true;

            for (int r = 0; r < board.Height; r++)
            for (int c = 0; c < board.Width; c++)
            {
                var cell = board.Grid[r, c];
                if (cell == null || cell.Value.cell_type == CellType.Obstacle || cell.Value.cell_type == CellType.Void) continue;
                remaining++;
                if (board.HasCore && cell.Value.is_core) coreCleared = false;
            }

            if (remaining == 0) return StarResult.Star3;

            float ratio = (board.InitialValidCells - remaining) / (float)board.InitialValidCells;

            if (!coreCleared || ratio < star1Ratio) return StarResult.Fail;
            if (ratio >= star2Ratio) return StarResult.Star2;
            return StarResult.Star1;
        }
    }
}
