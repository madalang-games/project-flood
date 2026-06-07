namespace Game.OutGame.Lobby
{
    public static class ScrollStateCache
    {
        public static float HomeScrollPosition { get; set; } = 0f;
        public static int   LastPlayedStageId  { get; set; } = 1;
        public static bool  UseExtraTurnsItem  { get; set; } = false;
    }
}
