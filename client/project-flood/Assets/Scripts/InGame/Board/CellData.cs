using ProjectFlood.Contracts.GameTypes;

namespace Game.InGame.Board
{
    public struct CellData
    {
        public int color_id;
        public CellType cell_type;
        public int protector_strength; // 0–2
        public bool is_core;
    }
}
