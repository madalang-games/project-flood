using ProjectFlood.Generated.Data;

namespace ProjectFlood.Application.Stamina;

public sealed class StaminaRuntimeConfig
{
    public int MaxLife { get; init; }
    public int RegenSeconds { get; init; }
    public int AttemptTimeoutSeconds { get; init; }
    public int MaxRevivePerAttempt { get; init; }
    public int[] ReviveTurns { get; init; } = Array.Empty<int>();
    public int AdLifeRewardAmount { get; init; }
    public string DailyResetTimezone { get; init; } = string.Empty;
    public int DailyResetHour { get; init; }
    public string DefaultUnlimitedStackPolicy { get; init; } = string.Empty;
}

public sealed class StaminaConfigProvider
{
    private readonly Lazy<StaminaRuntimeConfig> _config;

    public StaminaConfigProvider()
    {
        _config = new Lazy<StaminaRuntimeConfig>(Load);
    }

    public StaminaRuntimeConfig Current => _config.Value;

    private static StaminaRuntimeConfig Load()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "generated", "data", "stamina", "stamina_config.csv");
        var row = StaminaConfigLoader.LoadAll(path).FirstOrDefault(r => r.config_id == "default")
            ?? throw new InvalidOperationException($"Stamina config not found: {path}");

        return new StaminaRuntimeConfig
        {
            MaxLife = row.max_life,
            RegenSeconds = row.regen_seconds,
            AttemptTimeoutSeconds = row.attempt_timeout_seconds,
            MaxRevivePerAttempt = row.max_revive_per_attempt,
            ReviveTurns = new[] { row.revive_turn_1, row.revive_turn_2, row.revive_turn_3 },
            AdLifeRewardAmount = row.ad_life_reward_amount,
            DailyResetTimezone = row.daily_reset_timezone,
            DailyResetHour = row.daily_reset_hour,
            DefaultUnlimitedStackPolicy = row.default_unlimited_stack_policy,
        };
    }
}
