using Game.InGame.Board;

namespace Game.InGame.Rules
{
    public static class ProtectorSystem
    {
        // Returns true if cell should be removed (protector_strength was already 0)
        public static bool DirectHit(ref CellData cell)
        {
            if (cell.protector_strength > 0)
            {
                cell.protector_strength--;
                return false;
            }
            return true;
        }
    }
}
