namespace Game.OutGame.Lobby
{
    public static class ScrollStateCache
    {
        public static float HomeScrollPosition { get; set; } = 0f;
        public static int   LastPlayedStageId  { get; set; } = 1;
        public static bool  UseExtraTurnsItem  { get; set; } = false;
        public static bool  UseStartingBomb    { get; set; } = false;
        public static bool  UseStartingHRocket { get; set; } = false;
        public static int   CurrentWinStreak   { get; set; } = 0;
    }
}
