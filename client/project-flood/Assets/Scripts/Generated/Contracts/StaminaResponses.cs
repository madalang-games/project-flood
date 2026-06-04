#nullable enable
using System;

namespace ProjectFlood.Contracts.Stamina
{
    public sealed class StaminaSnapshot
    {
        public int Current { get; set; }
        public int Max { get; set; }
        public DateTimeOffset? NextRechargeAt { get; set; }
        public DateTimeOffset? UnlimitedUntil { get; set; }
        public bool IsUnlimited { get; set; }
        public int RegenSeconds { get; set; }
    }

    public sealed class StaminaStatusResponse
    {
        public StaminaSnapshot Stamina { get; set; } = new StaminaSnapshot();
        public DateTimeOffset ServerTime { get; set; }
    }

    public sealed class StaminaAdLifeRewardResponse
    {
        public bool Granted { get; set; }
        public bool Duplicate { get; set; }
        public int Delta { get; set; }
        public StaminaSnapshot Stamina { get; set; } = new StaminaSnapshot();
        public DateTimeOffset ServerTime { get; set; }
    }
}
