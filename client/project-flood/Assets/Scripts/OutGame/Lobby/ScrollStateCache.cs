using UnityEngine;

namespace Game.OutGame.Lobby
{
    public static class ScrollStateCache
    {
        private const string KeyScrollPos   = "hometab_scroll_pos";
        private const string KeyLastStageId = "hometab_last_stage_id";

        public static float HomeScrollPosition
        {
            get => PlayerPrefs.GetFloat(KeyScrollPos, 0f);
            set { PlayerPrefs.SetFloat(KeyScrollPos, value); PlayerPrefs.Save(); }
        }

        public static int LastPlayedStageId
        {
            get => PlayerPrefs.GetInt(KeyLastStageId, 0);
            set { PlayerPrefs.SetInt(KeyLastStageId, value); PlayerPrefs.Save(); }
        }

        // Session-only: not persisted across restarts
        public static bool UseExtraTurnsItem  { get; set; } = false;
        public static bool UseStartingBomb    { get; set; } = false;
        public static bool UseStartingHRocket { get; set; } = false;
        public static int  CurrentWinStreak   { get; set; } = 0;

        public static void Clear()
        {
            PlayerPrefs.DeleteKey(KeyScrollPos);
            PlayerPrefs.DeleteKey(KeyLastStageId);
            PlayerPrefs.Save();
            UseExtraTurnsItem  = false;
            UseStartingBomb    = false;
            UseStartingHRocket = false;
            CurrentWinStreak   = 0;
        }
    }
}
