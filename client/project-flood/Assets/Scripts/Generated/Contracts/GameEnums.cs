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

    public enum ItemUseType
    {
        PreGame = 0,
        InGame  = 1,
    }

    public enum ItemEffectType
    {
        AddTurns   = 0,
        Bomb       = 1,
        HRocket    = 2,
        ColorSweep = 3,
        RowShift   = 4,
        CellSwap   = 5,
    }

    public enum TutorialTriggerType
    {
        FirstLaunch = 0,
        GimmickAppear = 1,
        FailRepeat = 2,
    }

    public enum TutorialContentType
    {
        FingerOverlay = 0,
        Tooltip = 1,
        HighlightOnly = 2,
    }

    public enum TargetSpaceType
    {
        UI = 0,
        World = 1,
    }
}
