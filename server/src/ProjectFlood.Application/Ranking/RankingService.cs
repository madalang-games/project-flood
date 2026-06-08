using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProjectFlood.Application.Common;
using ProjectFlood.Contracts.Ranking;
using ProjectFlood.Contracts.Stage;
using ProjectFlood.Infrastructure.Generated;
using StackExchange.Redis;

namespace ProjectFlood.Application.Ranking;

public sealed class RankingService
{
    private const int DefaultLimit = 50;
    private const int MaxLimit = 100;
    private const double GlobalScoreFactor = 10_000_000_000d;

    private readonly AppDbContext _db;
    private readonly IDatabase _redis;
    private readonly ILogger<RankingService> _logger;

    public RankingService(AppDbContext db, IConnectionMultiplexer redis, ILogger<RankingService> logger)
    {
        _db = db;
        _redis = redis.GetDatabase();
        _logger = logger;
    }

    public async Task<StageClearRankingResult> RecordClearAsync(
        long userId,
        int stageId,
        string attemptId,
        StageAttemptClearRequest request,
        StageClearEvaluation evaluation,
        string correlationId,
        DateTimeOffset now,
        CancellationToken ct)
    {
        var progress = await _db.UserStageProgress.FindAsync(userId, stageId, ct);
        if (progress is null)
        {
            progress = new UserStageProgressRow
            {
                UserId = userId,
                StageId = stageId,
                BestStar = 0,
                UpdatedAt = now,
            };
            _db.UserStageProgress.Insert(progress);
        }

        var previousBestStar = progress.BestStar;
        var isNewBest = !progress.BestTurnsUsed.HasValue || request.TurnsUsed < progress.BestTurnsUsed.Value;

        if (!progress.FirstClearedAt.HasValue)
            progress.FirstClearedAt = now;
        if (evaluation.Stars > progress.BestStar)
        {
            progress.BestStar = evaluation.Stars;
            progress.BestStarUpdatedAt = now;
        }
        if (isNewBest)
        {
            progress.BestTurnsUsed = request.TurnsUsed;
            progress.BestTurnsUpdatedAt = now;
        }
        progress.UpdatedAt = now;

        var total = await _db.UserRankingTotals.FindAsync(userId, ct);
        if (total is null)
        {
            total = new UserRankingTotalsRow
            {
                UserId = userId,
                UpdatedAt = now,
            };
            _db.UserRankingTotals.Insert(total);
        }

        var starDelta = Math.Max(0, progress.BestStar - previousBestStar);
        if (starDelta > 0)
        {
            total.TotalEarnedStars += starDelta;
            total.TotalStarsAchievedAt = now;
        }

        if (stageId > total.MaxClearedStageId)
        {
            total.MaxClearedStageId = stageId;
            total.MaxStageAchievedAt = now;
        }
        total.UpdatedAt = now;

        _db.StageClearRecords.Insert(new StageClearRecordsRow
        {
            UserId = userId,
            StageId = stageId,
            AttemptId = attemptId,
            RulesetVersion = request.RulesetVersion,
            TurnsUsed = request.TurnsUsed,
            RemainingBasicCells = request.RemainingBasicCells,
            TotalBasicCells = evaluation.TotalBasicCells,
            CoreRemaining = request.CoreRemaining,
            Stars = evaluation.Stars,
            IsNewBest = isNewBest,
            CorrelationId = correlationId,
            CreatedAt = now,
        });

        return new StageClearRankingResult(
            evaluation.Stars,
            request.TurnsUsed,
            progress.BestTurnsUsed,
            isNewBest,
            total.TotalEarnedStars,
            total.TotalStarsAchievedAt ?? now,
            total.MaxClearedStageId,
            total.MaxStageAchievedAt ?? now);
    }

    public async Task<int?> GetStageRankAsync(int stageId, int? bestTurnsUsed, CancellationToken ct)
    {
        if (!bestTurnsUsed.HasValue)
            return null;

        var key = StageTurnsKey(stageId);
        try
        {
            if (await _redis.KeyExistsAsync(key))
            {
                var better = bestTurnsUsed.Value <= 0
                    ? 0
                    : await _redis.SortedSetLengthAsync(key, double.NegativeInfinity, bestTurnsUsed.Value - 1);
                return (int)better + 1;
            }
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(ex, "Stage ranking Redis read failed. Falling back to DB.");
        }

        var betterFromDb = await _db.UserStageProgress.Query()
            .CountAsync(x => x.StageId == stageId
                && x.BestTurnsUsed.HasValue
                && x.BestTurnsUsed.Value < bestTurnsUsed.Value, ct);
        return betterFromDb + 1;
    }

    public void QueueRedisUpdate(long userId, int stageId, StageClearRankingResult result)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await UpdateRedisForUserAsync(userId, stageId, result);
            }
            catch (Exception ex) when (ex is RedisException or RedisConnectionException)
            {
                _logger.LogWarning(ex, "Ranking Redis update failed after DB commit.");
            }
        });
    }

    public async Task<RankingPageResponse> GetGlobalPageAsync(string type, int offset, int limit, CancellationToken ct)
    {
        var rankingType = NormalizeGlobalType(type);
        offset = Math.Max(0, offset);
        limit = ClampLimit(limit);
        await EnsureGlobalKeyAsync(rankingType, ct);

        var entries = await _redis.SortedSetRangeByRankWithScoresAsync(
            GlobalKey(rankingType),
            offset,
            offset + limit - 1,
            Order.Ascending);

        var userIds = entries.Select(x => long.Parse(x.Element.ToString())).ToArray();
        var rows = await LoadGlobalRowsAsync(rankingType, userIds, ct);

        var response = new RankingPageResponse
        {
            RankingType = ToContractType(rankingType),
            Offset = offset,
            Limit = limit,
        };

        for (var i = 0; i < userIds.Length; i++)
        {
            if (!rows.TryGetValue(userIds[i], out var row))
                continue;

            response.Entries.Add(new RankingEntryDto
            {
                UserId = userIds[i],
                DisplayName = row.DisplayName,
                AvatarId = row.AvatarId,
                Rank = offset + i + 1,
                Score = row.Score,
            });
        }

        return response;
    }

    public async Task<MyRankingResponse> GetMyGlobalRankAsync(long userId, string type, CancellationToken ct)
    {
        var rankingType = NormalizeGlobalType(type);
        await EnsureGlobalKeyAsync(rankingType, ct);

        var rank = await _redis.SortedSetRankAsync(GlobalKey(rankingType), userId, Order.Ascending);
        if (!rank.HasValue)
        {
            return new MyRankingResponse
            {
                RankingType = ToContractType(rankingType),
                Entry = null,
            };
        }

        var rows = await LoadGlobalRowsAsync(rankingType, new[] { userId }, ct);
        if (!rows.TryGetValue(userId, out var row))
            return new MyRankingResponse { RankingType = ToContractType(rankingType), Entry = null };

        return new MyRankingResponse
        {
            RankingType = ToContractType(rankingType),
            Entry = new RankingEntryDto
            {
                UserId = userId,
                DisplayName = row.DisplayName,
                AvatarId = row.AvatarId,
                Rank = (int)rank.Value + 1,
                Score = row.Score,
            },
        };
    }

    public async Task<StageRankResponse> GetMyStageRankAsync(long userId, int stageId, CancellationToken ct)
    {
        var progress = await _db.UserStageProgress.FindAsync(userId, stageId, ct);
        var rank = await GetStageRankAsync(stageId, progress?.BestTurnsUsed, ct);
        return new StageRankResponse
        {
            StageId = stageId,
            Rank = rank,
            BestTurnsUsed = progress?.BestTurnsUsed,
        };
    }

    public async Task RebuildAllAsync(CancellationToken ct)
    {
        await RebuildGlobalAsync(ct);

        var stageIds = await _db.UserStageProgress.Query()
            .Where(x => x.BestTurnsUsed.HasValue)
            .Select(x => x.StageId)
            .Distinct()
            .ToListAsync(ct);

        foreach (var stageId in stageIds)
            await RebuildStageAsync(stageId, ct);
    }

    private async Task UpdateRedisForUserAsync(long userId, int stageId, StageClearRankingResult result)
    {
        if (result.BestTurnsUsed.HasValue)
            await _redis.SortedSetAddAsync(StageTurnsKey(stageId), userId, result.BestTurnsUsed.Value);

        await _redis.SortedSetAddAsync(GlobalKey(GlobalRankingType.Stars), userId, ComposeGlobalScore(result.TotalEarnedStars, result.TotalStarsAchievedAt));
        await _redis.SortedSetAddAsync(GlobalKey(GlobalRankingType.MaxStage), userId, ComposeGlobalScore(result.MaxClearedStageId, result.MaxStageAchievedAt));
    }

    private async Task EnsureGlobalKeyAsync(GlobalRankingType type, CancellationToken ct)
    {
        if (!await _redis.KeyExistsAsync(GlobalKey(type)))
            await RebuildGlobalAsync(ct);
    }

    private async Task RebuildGlobalAsync(CancellationToken ct)
    {
        var totals = await _db.UserRankingTotals.Query().ToListAsync(ct);
        await _redis.KeyDeleteAsync(GlobalKey(GlobalRankingType.Stars));
        await _redis.KeyDeleteAsync(GlobalKey(GlobalRankingType.MaxStage));

        if (totals.Count == 0)
            return;

        await _redis.SortedSetAddAsync(
            GlobalKey(GlobalRankingType.Stars),
            totals
                .Where(x => x.TotalEarnedStars > 0)
                .Select(x => new SortedSetEntry(x.UserId, ComposeGlobalScore(x.TotalEarnedStars, x.TotalStarsAchievedAt)))
                .ToArray());

        await _redis.SortedSetAddAsync(
            GlobalKey(GlobalRankingType.MaxStage),
            totals
                .Where(x => x.MaxClearedStageId > 0)
                .Select(x => new SortedSetEntry(x.UserId, ComposeGlobalScore(x.MaxClearedStageId, x.MaxStageAchievedAt)))
                .ToArray());
    }

    private async Task RebuildStageAsync(int stageId, CancellationToken ct)
    {
        var rows = await _db.UserStageProgress.Query()
            .Where(x => x.StageId == stageId && x.BestTurnsUsed.HasValue)
            .ToListAsync(ct);

        var key = StageTurnsKey(stageId);
        await _redis.KeyDeleteAsync(key);
        if (rows.Count == 0)
            return;

        await _redis.SortedSetAddAsync(
            key,
            rows.Select(x => new SortedSetEntry(x.UserId, x.BestTurnsUsed!.Value)).ToArray());
    }

    private async Task<Dictionary<long, GlobalRankingRow>> LoadGlobalRowsAsync(GlobalRankingType type, long[] userIds, CancellationToken ct)
    {
        if (userIds.Length == 0)
            return new Dictionary<long, GlobalRankingRow>();

        if (type == GlobalRankingType.Stars)
            return await _db.UserRankingTotals.Query()
                .Where(x => userIds.Contains(x.UserId))
                .Select(x => new GlobalRankingRow(x.UserId, x.Player!.DisplayName, x.Player!.AvatarId, x.TotalEarnedStars))
                .ToDictionaryAsync(x => x.UserId, ct);

        return await _db.UserRankingTotals.Query()
            .Where(x => userIds.Contains(x.UserId))
            .Select(x => new GlobalRankingRow(x.UserId, x.Player!.DisplayName, x.Player!.AvatarId, x.MaxClearedStageId))
            .ToDictionaryAsync(x => x.UserId, ct);
    }

    private static double ComposeGlobalScore(int primaryScore, DateTimeOffset? achievedAt)
        => -primaryScore * GlobalScoreFactor + (achievedAt ?? DateTimeOffset.MaxValue).ToUnixTimeSeconds();

    private static int ClampLimit(int limit)
        => limit <= 0 ? DefaultLimit : Math.Min(limit, MaxLimit);

    private static string StageTurnsKey(int stageId) => $"ranking:stage:{stageId}:turns";

    private static RedisKey GlobalKey(GlobalRankingType type)
        => type == GlobalRankingType.Stars ? "ranking:global:stars" : "ranking:global:max_stage";

    private static GlobalRankingType NormalizeGlobalType(string type)
        => type switch
        {
            "stars" => GlobalRankingType.Stars,
            "max-stage" or "max_stage" => GlobalRankingType.MaxStage,
            _ => throw new GameApiException(ErrorCodes.InvalidRankingType, "Invalid ranking type."),
        };

    private static string ToContractType(GlobalRankingType type)
        => type == GlobalRankingType.Stars ? "stars" : "max-stage";

    private enum GlobalRankingType
    {
        Stars,
        MaxStage,
    }

    private sealed record GlobalRankingRow(long UserId, string DisplayName, int AvatarId, int Score);
}

public sealed record StageClearEvaluation(int Stars, int TotalBasicCells);

public sealed record StageClearRankingResult(
    int Stars,
    int TurnsUsed,
    int? BestTurnsUsed,
    bool IsNewBest,
    int TotalEarnedStars,
    DateTimeOffset TotalStarsAchievedAt,
    int MaxClearedStageId,
    DateTimeOffset MaxStageAchievedAt);
