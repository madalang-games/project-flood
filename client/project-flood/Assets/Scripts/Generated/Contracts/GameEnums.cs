#nullable enable
namespace ProjectFlood.Contracts.GameTypes
{
    public enum CellType
    {
        Basic    = 0x0,
        Obstacle = 0x1,
        Void     = 0x2,
    }

    public enum Difficulty
    {
        Easy   = 0,
        Normal = 1,
        Hard   = 2,
    }
}
