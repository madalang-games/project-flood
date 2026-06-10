using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ProjectFlood.Application.Currency;
using ProjectFlood.Application.Player;
using ProjectFlood.Contracts.Player;
using ProjectFlood.Domain.Interfaces;
using ProjectFlood.Domain.StaticData;
using ProjectFlood.Infrastructure.Generated;
using ProjectFlood.Application.Common;
using Xunit;

namespace ProjectFlood.API.Tests
{
    public sealed class PlayerServiceTests
    {
        private AppDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .ConfigureWarnings(x => x.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
                .Options;
            return new AppDbContext(options);
        }

        private sealed class FakeStaticDataService : IStaticDataService
        {
            private readonly Dictionary<int, AvatarData> _avatars = new()
            {
                { 1, new AvatarData { AvatarId = 1, ResourceName = "avatar_free_01", UnlockCost = 0, UnlockType = "free" } },
                { 2, new AvatarData { AvatarId = 2, ResourceName = "avatar_gold_01", UnlockCost = 500, UnlockType = "gold" } },
                { 3, new AvatarData { AvatarId = 3, ResourceName = "avatar_silver_01", UnlockCost = 250, UnlockType = "silver" } },
                { 4, new AvatarData { AvatarId = 4, ResourceName = "avatar_achievement_01", UnlockCost = 0, UnlockType = "achievement" } }
            };

            private readonly Dictionary<int, BoardThemeData> _boardThemes = new()
            {
                { 1, new BoardThemeData { ThemeId = 1, ResourceName = "classic", UnlockCost = 0, UnlockType = "free", DisplayName = "Classic" } },
                { 2, new BoardThemeData { ThemeId = 2, ResourceName = "neon", UnlockCost = 1000, UnlockType = "gold", DisplayName = "Neon" } },
                { 3, new BoardThemeData { ThemeId = 3, ResourceName = "wood", UnlockCost = 500, UnlockType = "silver", DisplayName = "Wood" } },
                { 4, new BoardThemeData { ThemeId = 4, ResourceName = "cyberpunk", UnlockCost = 0, UnlockType = "achievement", DisplayName = "Cyberpunk" } }
            };

            public AvatarData? GetAvatar(int avatar_id) => _avatars.GetValueOrDefault(avatar_id);
            public IReadOnlyList<AvatarData> GetAllAvatars() => new List<AvatarData>(_avatars.Values);

            public BoardThemeData? GetBoardTheme(int theme_id) => _boardThemes.GetValueOrDefault(theme_id);
            public IReadOnlyList<BoardThemeData> GetAllBoardThemes() => new List<BoardThemeData>(_boardThemes.Values);

            public AdPlacementData? GetAdPlacement(string placement_id) => throw new NotImplementedException();
            public IReadOnlyList<AdPlacementData> GetAllAdPlacements() => throw new NotImplementedException();
            public ColorPaletteData? GetColorPalette(byte color_id) => throw new NotImplementedException();
            public IReadOnlyList<ColorPaletteData> GetAllColorPalettes() => throw new NotImplementedException();
            public DynamicResourceData? GetDynamicResource(string resource_key) => throw new NotImplementedException();
            public IReadOnlyList<DynamicResourceData> GetAllDynamicResources() => throw new NotImplementedException();
            public ItemData? GetItem(int item_id) => throw new NotImplementedException();
            public IReadOnlyList<ItemData> GetAllItems() => throw new NotImplementedException();
            public RewardGroupData? GetRewardGroup(int reward_group_id) => throw new NotImplementedException();
            public IReadOnlyList<RewardGroupData> GetAllRewardGroups() => throw new NotImplementedException();
            public RewardSourceData? GetRewardSource(string source_id) => throw new NotImplementedException();
            public IReadOnlyList<RewardSourceData> GetAllRewardSources() => throw new NotImplementedException();
            public ChapterData? GetChapter(int chapter_id) => throw new NotImplementedException();
            public IReadOnlyList<ChapterData> GetAllChapters() => throw new NotImplementedException();
            public StageData? GetStage(int stage_id) => throw new NotImplementedException();
            public IReadOnlyList<StageData> GetAllStages() => throw new NotImplementedException();
            public StaminaConfigData? GetStaminaConfig(string config_id) => throw new NotImplementedException();
            public IReadOnlyList<StaminaConfigData> GetAllStaminaConfigs() => throw new NotImplementedException();
        }

        [Fact]
        public async Task UpdateProfile_ValidNameAndFreeAvatar_Succeeds()
        {
            using var db = CreateDbContext();
            var staticData = new FakeStaticDataService();
            var service = new PlayerService(db, staticData, new CurrencyService(db));

            long userId = 1;
            db.Players.Insert(new PlayersRow
            {
                UserId = userId,
                PlatformPid = "pid1",
                DisplayName = "OldName",
                AvatarId = 1,
                AccountCreatedAt = DateTimeOffset.UtcNow,
                LastLoginAt = DateTimeOffset.UtcNow
            });
            await db.SaveAsync();

            var req = new UserProfileUpdateRequest
            {
                DisplayName = "Valid_Name-123",
                AvatarId = 1
            };

            var res = await service.UpdateProfileAsync(userId, req, "corr-id", default);
            Assert.Equal("Valid_Name-123", res.DisplayName);
            Assert.Equal(1, res.AvatarId);

            var player = await db.Players.FindAsync(userId);
            Assert.Equal("Valid_Name-123", player?.DisplayName);
        }

        [Fact]
        public async Task UpdateProfile_InvalidNicknameChars_ThrowsException()
        {
            using var db = CreateDbContext();
            var staticData = new FakeStaticDataService();
            var service = new PlayerService(db, staticData, new CurrencyService(db));

            long userId = 1;
            db.Players.Insert(new PlayersRow
            {
                UserId = userId,
                PlatformPid = "pid1",
                DisplayName = "OldName",
                AvatarId = 1,
                AccountCreatedAt = DateTimeOffset.UtcNow,
                LastLoginAt = DateTimeOffset.UtcNow
            });
            await db.SaveAsync();

            var req = new UserProfileUpdateRequest
            {
                DisplayName = "NameWith한글"
            };

            var ex = await Assert.ThrowsAsync<GameApiException>(() => service.UpdateProfileAsync(userId, req, "corr-id", default));
            Assert.Equal("INVALID_DISPLAY_NAME", ex.Code);
        }

        [Fact]
        public async Task UpdateProfile_GoldAvatarUnlock_DeductsGoldAndSucceeds()
        {
            using var db = CreateDbContext();
            var staticData = new FakeStaticDataService();
            var service = new PlayerService(db, staticData, new CurrencyService(db));

            long userId = 1;
            db.Players.Insert(new PlayersRow
            {
                UserId = userId,
                PlatformPid = "pid1",
                DisplayName = "PlayerName",
                AvatarId = 1,
                AccountCreatedAt = DateTimeOffset.UtcNow,
                LastLoginAt = DateTimeOffset.UtcNow
            });
            db.UserCurrency.Insert(new UserCurrencyRow
            {
                UserId = userId,
                SoftAmount = 1000,
                UpdatedAt = DateTimeOffset.UtcNow
            });
            await db.SaveAsync();

            var req = new UserProfileUpdateRequest { AvatarId = 2 }; // gold cost 500
            var res = await service.UpdateProfileAsync(userId, req, "corr-id", default);

            Assert.Equal(2, res.AvatarId);

            var currency = await db.UserCurrency.FindAsync(userId);
            Assert.Equal(500, currency?.SoftAmount);

            var isUnlocked = await db.UserRewardClaimState.Query()
                .AnyAsync(x => x.UserId == userId && x.SourceId == "avatar_unlock:2");
            Assert.True(isUnlocked);
        }

        [Fact]
        public async Task UpdateProfile_SilverAvatarUnlock_DeductsGoldAndSucceeds()
        {
            using var db = CreateDbContext();
            var staticData = new FakeStaticDataService();
            var service = new PlayerService(db, staticData, new CurrencyService(db));

            long userId = 1;
            db.Players.Insert(new PlayersRow
            {
                UserId = userId,
                PlatformPid = "pid1",
                DisplayName = "PlayerName",
                AvatarId = 1,
                AccountCreatedAt = DateTimeOffset.UtcNow,
                LastLoginAt = DateTimeOffset.UtcNow
            });
            db.UserCurrency.Insert(new UserCurrencyRow
            {
                UserId = userId,
                SoftAmount = 1000,
                UpdatedAt = DateTimeOffset.UtcNow
            });
            await db.SaveAsync();

            var req = new UserProfileUpdateRequest { AvatarId = 3 }; // silver cost 250
            var res = await service.UpdateProfileAsync(userId, req, "corr-id", default);

            Assert.Equal(3, res.AvatarId);

            var currency = await db.UserCurrency.FindAsync(userId);
            Assert.Equal(750, currency?.SoftAmount);

            var isUnlocked = await db.UserRewardClaimState.Query()
                .AnyAsync(x => x.UserId == userId && x.SourceId == "avatar_unlock:3");
            Assert.True(isUnlocked);
        }

        [Fact]
        public async Task GetProgress_IncludesUnlockedAvatars()
        {
            using var db = CreateDbContext();
            var staticData = new FakeStaticDataService();
            var service = new PlayerService(db, staticData, new CurrencyService(db));

            long userId = 1;
            db.UserRewardClaimState.Insert(new UserRewardClaimStateRow
            {
                UserId = userId,
                SourceId = "avatar_unlock:2",
                PeriodKey = "once",
                ClaimCount = 1,
                LastClaimedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            });
            db.UserRewardClaimState.Insert(new UserRewardClaimStateRow
            {
                UserId = userId,
                SourceId = "avatar_unlock:3",
                PeriodKey = "once",
                ClaimCount = 1,
                LastClaimedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            });
            db.UserRankingTotals.Insert(new UserRankingTotalsRow
            {
                UserId = userId,
                TotalEarnedStars = 10,
                TotalStarsAchievedAt = DateTimeOffset.UtcNow,
                MaxClearedStageId = 5,
                MaxStageAchievedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            });
            await db.SaveAsync();

            var progress = await service.GetProgressAsync(userId, default);
            Assert.Equal(5, progress.MaxClearedStageId);
            Assert.Equal(2, progress.UnlockedAvatarIds.Count);
            Assert.Contains(2, progress.UnlockedAvatarIds);
            Assert.Contains(3, progress.UnlockedAvatarIds);
        }
    }
}
