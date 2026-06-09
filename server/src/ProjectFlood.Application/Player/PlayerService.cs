using Microsoft.EntityFrameworkCore;
using ProjectFlood.Contracts.Player;
using ProjectFlood.Infrastructure.Generated;
using ProjectFlood.Domain.Interfaces;
using ProjectFlood.Application.Common;
using ProjectFlood.Application.Logging;

namespace ProjectFlood.Application.Player;

public sealed class PlayerService
{
    private readonly AppDbContext _db;
    private readonly IStaticDataService _staticData;

    public PlayerService(AppDbContext db, IStaticDataService staticData)
    {
        _db = db;
        _staticData = staticData;
    }

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

    public async Task<UserProfileUpdateResponse> UpdateProfileAsync(
        long userId,
        UserProfileUpdateRequest request,
        string correlationId,
        CancellationToken ct)
    {
        var player = await _db.Players.FindAsync(userId, ct);
        if (player == null)
            throw new GameApiException("PLAYER_NOT_FOUND", "Player not found.");

        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        if (!string.IsNullOrWhiteSpace(request.DisplayName))
        {
            var cleanName = request.DisplayName.Trim();
            if (cleanName.Length is < 2 or > 24)
                throw new GameApiException("INVALID_DISPLAY_NAME", "Display name must be between 2 and 24 characters.");
            player.DisplayName = cleanName;
        }

        if (request.AvatarId.HasValue)
        {
            var avatarId = request.AvatarId.Value;
            if (avatarId != player.AvatarId)
            {
                var avatarData = _staticData.GetAvatar(avatarId);
                if (avatarData == null)
                    throw new GameApiException("AVATAR_NOT_FOUND", "Avatar not found.");

                if (avatarData.UnlockType == "gold")
                {
                    var claimKey = $"avatar_unlock:{avatarId}";
                    var isUnlocked = await _db.UserRewardClaimState.Query()
                        .AnyAsync(x => x.UserId == userId && x.SourceId == claimKey, ct);

                    if (!isUnlocked)
                    {
                        var currency = await _db.UserCurrency.FindAsync(userId, ct);
                        if (currency == null || currency.SoftAmount < avatarData.UnlockCost)
                            throw new GameApiException("INSUFFICIENT_GOLD", "Not enough gold to unlock this avatar.");

                        currency.SoftAmount -= avatarData.UnlockCost;
                        currency.UpdatedAt = DateTimeOffset.UtcNow;

                        _db.UserRewardClaimState.Insert(new UserRewardClaimStateRow
                        {
                            UserId = userId,
                            SourceId = claimKey,
                            PeriodKey = "once",
                            ClaimCount = 1,
                            LastClaimedAt = DateTimeOffset.UtcNow,
                            UpdatedAt = DateTimeOffset.UtcNow
                        });

                        _db.EventLogs.Insert(EventLogFactory.CurrencyChanged(userId, correlationId, -avatarData.UnlockCost, "avatar_unlock", currency.SoftAmount));
                    }
                }
                else if (avatarData.UnlockType == "achievement")
                {
                    var claimKey = $"avatar_unlock:{avatarId}";
                    var isUnlocked = await _db.UserRewardClaimState.Query()
                        .AnyAsync(x => x.UserId == userId && x.SourceId == claimKey, ct);

                    if (!isUnlocked)
                        throw new GameApiException("AVATAR_LOCKED", "This avatar must be unlocked via achievements.");
                }

                player.AvatarId = avatarId;
            }
        }

        player.LastLoginAt = DateTimeOffset.UtcNow;
        await _db.SaveAsync(ct);
        await tx.CommitAsync(ct);

        return new UserProfileUpdateResponse
        {
            DisplayName = player.DisplayName,
            AvatarId = player.AvatarId
        };
    }
}
