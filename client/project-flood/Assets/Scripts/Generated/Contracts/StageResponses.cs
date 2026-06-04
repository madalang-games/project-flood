#nullable enable
using System;
using ProjectFlood.Contracts.Stamina;

namespace ProjectFlood.Contracts.Stage
{
    public sealed class StageAttemptSnapshot
    {
        public string AttemptId { get; set; } = string.Empty;
        public int StageId { get; set; }
        public DateTimeOffset ExpiresAt { get; set; }
        public int ReviveCount { get; set; }
        public int RemainingRevives { get; set; }
        public bool LifeSpent { get; set; }
    }

    public sealed class StageAttemptStartResponse
    {
        public StageAttemptSnapshot Attempt { get; set; } = new StageAttemptSnapshot();
        public StaminaSnapshot Stamina { get; set; } = new StaminaSnapshot();
        public DateTimeOffset ServerTime { get; set; }
    }

    public sealed class StageAttemptEndResponse
    {
        public string AttemptId { get; set; } = string.Empty;
        public int StageId { get; set; }
        public string Result { get; set; } = string.Empty;
        public bool LifeRefunded { get; set; }
        public StaminaSnapshot Stamina { get; set; } = new StaminaSnapshot();
        public DateTimeOffset ServerTime { get; set; }
    }

    public sealed class StageReviveAdResponse
    {
        public bool Granted { get; set; }
        public bool Duplicate { get; set; }
        public int ReviveCount { get; set; }
        public int TurnsGranted { get; set; }
        public StageAttemptSnapshot Attempt { get; set; } = new StageAttemptSnapshot();
        public DateTimeOffset ServerTime { get; set; }
    }
}
