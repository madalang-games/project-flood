#nullable enable
namespace ProjectFlood.Contracts.Ranking
{
    public sealed class RankingPageRequest
    {
        public int Offset { get; set; }
        public int Limit { get; set; }
    }
}
