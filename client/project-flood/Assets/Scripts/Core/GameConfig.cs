namespace Game.Core
{
    public static class GameConfig
    {
        public const int  ContinueCost       = 150;
        public const int  ContinueExtraTurns = 3;
        public const float LoadingTimeoutSec = 10f;
        public const int  StageNodePoolSize     = 50;
        public const int  StageNodeRowOffset    = 300;  // row node x half-span
        public const int  StageNodeZigzagOffset = 450;  // connector x far offset (capped ~540 half-width)
    }
}
