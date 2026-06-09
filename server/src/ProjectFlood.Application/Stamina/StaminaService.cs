using Microsoft.EntityFrameworkCore;
using ProjectFlood.Application.Common;
using ProjectFlood.Application.Logging;
using ProjectFlood.Application.Rewards;
using ProjectFlood.Contracts.Stamina;
using ProjectFlood.Infrastructure.Generated;
using StackExchange.Redis;
using System.Text.Json;

namespace ProjectFlood.Application.Stamina;

public sealed class StaminaService
{
    private readonly AppDbContext _db;
    private readonly StaminaConfigProvider _config;
    private readonly IAdRewardVerifier _adVerifier;
    private readonly IDatabase _redis;

    public StaminaService(AppDbContext db, StaminaConfigProvider config, IAdRewardVerifier adVerifier, IConnectionMultiplexer redis)
    {
        _db = db;
        _config = config;
        _adVerifier = adVerifier;
        _redis = redis.GetDatabase();
    }

    public async Task<StaminaStatusResponse> GetAsync(long userId, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var state = await EnsureStateAsync(userId, now, ct);
        ApplyRegen(state, now);
        await _db.SaveAsync(ct);

        return new StaminaStatusResponse
        {
            Stamina = ToSnapshot(state, now),
            ServerTime = now,
        };
    }

    public async Task<StaminaAdLifeRewardResponse> GrantAdLifeAsync(
        long userId,
        string provider,
        string adToken,
        string correlationId,
        CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var state = await EnsureStateAsync(userId, now, ct);
        ApplyRegen(state, now);

        var config = _config.Current;
        if (state.CurrentLife >= config.MaxLife)
            throw new GameApiException(ErrorCodes.StaminaFull, "Stamina is already full.");

        var result = await _adVerifier.VerifyAsync(provider, adToken, ct);
        if (!result.Verified)
        {
            var pending = new PendingAdClaim
            {
                UserId = userId,
                PlacementId = "STAMINA_LIFE",
                Provider = provider,
                AdToken = adToken,
                ContextType = "home",
                ContextId = string.Empty,
                RequestJson = string.Empty,
                CorrelationId = correlationId
            };
            await _redis.StringSetAsync($"pending_claim:{adToken}", JsonSerializer.Serialize(pending), TimeSpan.FromMinutes(5));
            throw new GameApiException(ErrorCodes.AdSsvPending, "Ad SSV callback not yet received.");
        }

        var existing = await FindAdTransactionAsync(provider, result.ProviderTxId, ct);
        if (existing is not null)
        {
            if (existing.UserId != userId || existing.PlacementId != "STAMINA_LIFE")
                throw new GameApiException(ErrorCodes.AdRewardDuplicate, "Ad reward transaction is already owned by another context.");

            await _db.SaveAsync(ct);
            return new StaminaAdLifeRewardResponse
            {
                Granted = false,
                Duplicate = true,
                Delta = 0,
                Stamina = ToSnapshot(state, now),
                ServerTime = now,
            };
        }

        var before = state.CurrentLife;
        state.CurrentLife = Math.Min(config.MaxLife, state.CurrentLife + config.AdLifeRewardAmount);
        state.UpdatedAt = now;
        var delta = state.CurrentLife - before;

        var tx = new AdRewardTransactionsRow
        {
            Id = Guid.NewGuid().ToString("N"),
            UserId = userId,
            PlacementId = "STAMINA_LIFE",
            RewardType = "STAMINA_LIFE",
            RewardValue = delta,
            ContextType = "home",
            ContextId = null,
            Provider = provider,
            ProviderTxId = result.ProviderTxId,
            Status = "granted",
            CorrelationId = correlationId,
            CreatedAt = now,
            VerifiedAt = now,
            GrantedAt = now,
        };
        _db.AdRewardTransactions.Insert(tx);
        _db.EventLogs.Insert(EventLogFactory.AdRewardClaimed(userId, correlationId, tx.Id, tx.PlacementId, tx.RewardType, tx.RewardValue, false));
        _db.EventLogs.Insert(EventLogFactory.StaminaLifeChanged(userId, correlationId, delta, "ad_life", state.CurrentLife));
        await _db.SaveAsync(ct);

        return new StaminaAdLifeRewardResponse
        {
            Granted = true,
            Duplicate = false,
            Delta = delta,
            Stamina = ToSnapshot(state, now),
            ServerTime = now,
        };
    }

    public async Task<(StaminaSnapshot Snapshot, bool LifeSpent)> SpendForAttemptAsync(long userId, string correlationId, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var state = await EnsureStateAsync(userId, now, ct);
        ApplyRegen(state, now);
        if (IsUnlimited(state, now))
            return (ToSnapshot(state, now), false);

        if (state.CurrentLife <= 0)
            throw new GameApiException(ErrorCodes.InsufficientStamina, "Not enough stamina.");

        state.CurrentLife--;
        state.UpdatedAt = now;
        _db.EventLogs.Insert(EventLogFactory.StaminaLifeChanged(userId, correlationId, -1, "spend", state.CurrentLife));
        return (ToSnapshot(state, now), true);
    }

    public async Task<StaminaSnapshot> RefundAttemptLifeAsync(long userId, string correlationId, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var state = await EnsureStateAsync(userId, now, ct);
        ApplyRegen(state, now);
        var config = _config.Current;
        var before = state.CurrentLife;
        state.CurrentLife = Math.Min(config.MaxLife, state.CurrentLife + 1);
        state.UpdatedAt = now;
        var delta = state.CurrentLife - before;
        if (delta != 0)
            _db.EventLogs.Insert(EventLogFactory.StaminaLifeChanged(userId, correlationId, delta, "refund", state.CurrentLife));
        return ToSnapshot(state, now);
    }

    public async Task<StaminaSnapshot> GrantUnlimitedAsync(long userId, string sourceId, int durationSeconds, string correlationId, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var state = await EnsureStateAsync(userId, now, ct);
        ApplyRegen(state, now);
        var baseTime = state.UnlimitedUntil.HasValue && state.UnlimitedUntil.Value > now
            ? state.UnlimitedUntil.Value
            : now;
        state.UnlimitedUntil = baseTime.AddSeconds(durationSeconds);
        state.UpdatedAt = now;
        _db.EventLogs.Insert(EventLogFactory.StaminaUnlimitedChanged(userId, correlationId, sourceId, durationSeconds, state.UnlimitedUntil.Value));
        return ToSnapshot(state, now);
    }

    private async Task<UserStaminaStateRow> EnsureStateAsync(long userId, DateTimeOffset now, CancellationToken ct)
    {
        var row = await _db.UserStaminaState.FindAsync(userId, ct);
        if (row is not null) return row;

        row = new UserStaminaStateRow
        {
            UserId = userId,
            CurrentLife = _config.Current.MaxLife,
            LastRegenAt = now,
            UnlimitedUntil = null,
            LastDailyUnlimitedClaimedDate = null,
            UpdatedAt = now,
        };
        _db.UserStaminaState.Insert(row);
        return row;
    }

    private void ApplyRegen(UserStaminaStateRow state, DateTimeOffset now)
    {
        var config = _config.Current;
        if (state.CurrentLife >= config.MaxLife)
        {
            state.LastRegenAt = now;
            return;
        }

        var ticks = (int)((now - state.LastRegenAt).TotalSeconds / config.RegenSeconds);
        if (ticks <= 0) return;

        state.CurrentLife = Math.Min(config.MaxLife, state.CurrentLife + ticks);
        state.LastRegenAt = state.CurrentLife >= config.MaxLife
            ? now
            : state.LastRegenAt.AddSeconds(ticks * config.RegenSeconds);
        state.UpdatedAt = now;
    }

    private StaminaSnapshot ToSnapshot(UserStaminaStateRow state, DateTimeOffset now)
    {
        var config = _config.Current;
        return new StaminaSnapshot
        {
            Current = state.CurrentLife,
            Max = config.MaxLife,
            NextRechargeAt = state.CurrentLife < config.MaxLife ? state.LastRegenAt.AddSeconds(config.RegenSeconds) : null,
            UnlimitedUntil = state.UnlimitedUntil,
            IsUnlimited = IsUnlimited(state, now),
            RegenSeconds = config.RegenSeconds,
        };
    }

    private static bool IsUnlimited(UserStaminaStateRow state, DateTimeOffset now)
        => state.UnlimitedUntil.HasValue && state.UnlimitedUntil.Value > now;

    private Task<AdRewardTransactionsRow?> FindAdTransactionAsync(string provider, string providerTransactionId, CancellationToken ct)
        => _db.AdRewardTransactions.Query()
            .FirstOrDefaultAsync(x => x.Provider == provider && x.ProviderTxId == providerTransactionId, ct);
}
