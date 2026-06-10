#nullable enable
using System.Collections.Generic;

namespace ProjectFlood.Contracts.Player
{
    public sealed class PlayerProgressResponse
    {
        public int MaxClearedStageId { get; set; }
        public List<StageProgressEntry> Stages { get; set; } = new List<StageProgressEntry>();
        public List<int> UnlockedAvatarIds { get; set; } = new List<int>();
        public int EquippedBoardThemeId { get; set; }
        public List<int> UnlockedBoardThemeIds { get; set; } = new List<int>();
    }

    public sealed class StageProgressEntry
    {
        public int StageId { get; set; }
        public int BestStar { get; set; }
    }
}
