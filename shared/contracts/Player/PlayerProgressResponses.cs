#nullable enable
using System.Collections.Generic;

namespace ProjectFlood.Contracts.Player
{
    public sealed class PlayerProgressResponse
    {
        public int MaxClearedStageId { get; set; }
        public List<StageProgressEntry> Stages { get; set; } = new List<StageProgressEntry>();
    }

    public sealed class StageProgressEntry
    {
        public int StageId { get; set; }
        public int BestStar { get; set; }
    }
}
