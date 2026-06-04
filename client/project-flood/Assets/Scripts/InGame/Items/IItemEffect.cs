using System.Collections.Generic;
using Game.InGame.Board;

namespace Game.InGame.Items
{
    public interface IItemEffect
    {
        List<(int row, int col)> GetAffectedCells(BoardState board, int targetRow, int targetCol);
    }
}
