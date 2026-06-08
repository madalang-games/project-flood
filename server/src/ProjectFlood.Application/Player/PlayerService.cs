using Microsoft.EntityFrameworkCore;
using ProjectFlood.Contracts.Player;
using ProjectFlood.Infrastructure.Generated;

namespace ProjectFlood.Application.Player;

public sealed class PlayerService
{
    private readonly AppDbContext _db;

    public PlayerService(AppDbContext db) => _db = db;

    public async Task<PlayerProgressResponse> GetProgressAsync(long userId, CancellationToken ct)
    {
        var totals = await _db.UserRankingTotals.FindAsync(userId, ct);

        var stages = await _db.UserStageProgress.Query()
            .Where(s => s.UserId == userId && s.BestStar > 0)
            .Select(s => new StageProgressEntry { StageId = s.StageId, BestStar = s.BestStar })
            .ToListAsync(ct);

        return new PlayerProgressResponse
        {
            MaxClearedStageId = totals?.MaxClearedStageId ?? 0,
            Stages = stages,
        };
    }
}
