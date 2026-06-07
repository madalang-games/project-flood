using ProjectFlood.Application.Common;
using ProjectFlood.Application.Currency;
using ProjectFlood.Application.Logging;
using ProjectFlood.Application.Stamina;
using ProjectFlood.Contracts.Currency;
using ProjectFlood.Contracts.Rewards;
using ProjectFlood.Contracts.Stamina;
using ProjectFlood.Generated.Data;
using ProjectFlood.Infrastructure.Generated;

using ProjectFlood.Application.Inventory;

namespace ProjectFlood.Application.Rewards;

public sealed class RewardService
{
    private readonly AppDbContext _db;
    private readonly StaminaService _stamina;
    private readonly CurrencyService _currency;
    private readonly InventoryService _inventory;
    private readonly Lazy<RewardDataSet> _data;

    public RewardService(AppDbContext db, StaminaService stamina, CurrencyService currency, InventoryService inventory)
    {
        _db = db;
        _stamina = stamina;
        _currency = currency;
        _inventory = inventory;
        _data = new Lazy<RewardDataSet>(LoadData);
    }

    public async Task<RewardSourcesResponse> GetSourcesAsync(long userId, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var result = new RewardSourcesResponse { ServerTime = now };
        foreach (var source in _data.Value.Sources.Where(x => x.is_enabled))
        {
            var periodKey = GetPeriodKey(source, now);
            var state = await _db.UserRewardClaimState.FindAsync(userId, source.source_id, periodKey, ct);
            result.Sources.Add(new RewardSourceDto
            {
                SourceId = source.source_id,
                SourceType = source.source_type,
                RewardGroupId = source.reward_group_id,
                Claimable = state is null || state.ClaimCount < source.max_claims,
                NextAvailableAt = state is null || state.ClaimCount < source.max_claims ? null : GetNextDailyResetUtc(source, now),
                UiSurface = source.ui_surface,
            });
        }

        return result;
    }

    public async Task<RewardClaimResponse> ClaimAsync(long userId, string sourceId, string correlationId, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var source = _data.Value.Sources.FirstOrDefault(x => x.source_id == sourceId && x.is_enabled)
            ?? throw new GameApiException("REWARD_SOURCE_NOT_FOUND", "Reward source not found.");

        if (sourceId.StartsWith("chapter") && sourceId.EndsWith("_chest"))
        {
            int startStage = 1;
            int endStage = 3;
            if (sourceId == "chapter2_chest") { startStage = 4; endStage = 6; }
            else if (sourceId == "chapter3_chest") { startStage = 7; endStage = 9; }

            for (int stageId = startStage; stageId <= endStage; stageId++)
            {
                var progress = await _db.UserStageProgress.FindAsync(userId, stageId, ct);
                if (progress is null || progress.BestStar < 3)
                    throw new GameApiException("CHAPTER_NOT_COMPLETED", $"Stage {stageId} is not cleared with 3 stars.");
            }
        }

        var periodKey = GetPeriodKey(source, now);
        var state = await _db.UserRewardClaimState.FindAsync(userId, source.source_id, periodKey, ct);
        if (state is not null && state.ClaimCount >= source.max_claims)
            throw new GameApiException(ErrorCodes.RewardAlreadyClaimed, "Reward source already claimed.");

        if (state is null)
        {
            state = new UserRewardClaimStateRow
            {
                UserId = userId,
                SourceId = source.source_id,
                PeriodKey = periodKey,
                ClaimCount = 0,
                LastClaimedAt = now,
                UpdatedAt = now,
            };
            _db.UserRewardClaimState.Insert(state);
        }

        state.ClaimCount++;
        state.LastClaimedAt = now;
        state.UpdatedAt = now;

        var items = _data.Value.Items
            .Where(x => x.reward_group_id == source.reward_group_id && x.version == source.version)
            .OrderBy(x => x.sort_order)
            .ToList();
        var granted = new List<GrantedRewardDto>();
        StaminaSnapshot? stamina = null;

        foreach (var item in items)
        {
            granted.Add(new GrantedRewardDto
            {
                RewardType = item.reward_type,
                TargetId = item.target_id,
                Amount = item.amount,
                DurationSeconds = item.duration_seconds,
            });

            if (item.reward_type == "STAMINA_UNLIMITED")
                stamina = await _stamina.GrantUnlimitedAsync(userId, source.source_id, item.duration_seconds, correlationId, ct);
            else if (item.reward_type == "ITEM")
                await _inventory.GrantItemAsync(userId, item.target_id, item.amount, $"claim:{source.source_id}", correlationId, ct);
        }

        stamina ??= (await _stamina.GetAsync(userId, ct)).Stamina;

        _db.EventLogs.Insert(EventLogFactory.RewardClaimed(userId, correlationId, source.source_id, source.reward_group_id));
        await _db.SaveAsync(ct);

        return new RewardClaimResponse
        {
            SourceId = source.source_id,
            GrantedRewards = granted,
            Stamina = stamina,
            ServerTime = now,
        };
    }

    public async Task<(List<GrantedRewardDto> Granted, CurrencySnapshot? Currency)> GrantRewardGroupAsync(
        long userId,
        int rewardGroupId,
        int version,
        string correlationId,
        CancellationToken ct)
    {
        var items = _data.Value.Items
            .Where(x => x.reward_group_id == rewardGroupId && x.version == version)
            .OrderBy(x => x.sort_order)
            .ToList();

        var granted = new List<GrantedRewardDto>();
        CurrencySnapshot? currency = null;

        foreach (var item in items)
        {
            granted.Add(new GrantedRewardDto
            {
                RewardType = item.reward_type,
                TargetId = item.target_id,
                Amount = item.amount,
                DurationSeconds = item.duration_seconds,
            });

            switch (item.reward_type)
            {
                case "STAMINA_UNLIMITED":
                    await _stamina.GrantUnlimitedAsync(userId, $"reward_group:{rewardGroupId}", item.duration_seconds, correlationId, ct);
                    break;
                case "SOFT_CURRENCY":
                    currency = await _currency.GrantSoftAsync(userId, item.amount, $"reward_group:{rewardGroupId}", correlationId, ct);
                    break;
                case "ITEM":
                    await _inventory.GrantItemAsync(userId, item.target_id, item.amount, $"reward_group:{rewardGroupId}", correlationId, ct);
                    break;
            }
        }

        return (granted, currency);
    }

    private static RewardDataSet LoadData()
    {
        var root = Path.Combine(AppContext.BaseDirectory, "generated", "data", "reward");
        return new RewardDataSet(
            RewardSourceLoader.LoadAll(Path.Combine(root, "reward_source.csv")),
            RewardItemLoader.LoadAll(Path.Combine(root, "reward_item.csv")));
    }

    private static string GetPeriodKey(RewardSource source, DateTimeOffset utcNow)
        => source.claim_policy == "DAILY_RESET"
            ? ToKst(utcNow).ToString("yyyy-MM-dd")
            : "always";

    private static DateTimeOffset GetNextDailyResetUtc(RewardSource source, DateTimeOffset utcNow)
    {
        var kst = ToKst(utcNow);
        var next = new DateTimeOffset(kst.Year, kst.Month, kst.Day, source.reset_hour, 0, 0, kst.Offset);
        if (next <= kst) next = next.AddDays(1);
        return next.ToUniversalTime();
    }

    private static DateTimeOffset ToKst(DateTimeOffset utc)
        => TimeZoneInfo.ConvertTime(utc, GetKst());

    private static TimeZoneInfo GetKst()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("Asia/Seoul"); }
        catch (TimeZoneNotFoundException) { return TimeZoneInfo.FindSystemTimeZoneById("Korea Standard Time"); }
    }

    private sealed record RewardDataSet(IReadOnlyList<RewardSource> Sources, IReadOnlyList<RewardItem> Items);
}
