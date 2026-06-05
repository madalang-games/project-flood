#nullable enable
namespace ProjectFlood.Contracts.Stage
{
    public sealed class StageAttemptStartRequest
    {
        public string ClientRequestId { get; set; } = string.Empty;
    }

    public sealed class StageAttemptClearRequest
    {
        public string ClientRequestId { get; set; } = string.Empty;
        public int RulesetVersion { get; set; }
        public int TurnsUsed { get; set; }
        public int RemainingBasicCells { get; set; }
        public bool CoreRemaining { get; set; }
    }

    public sealed class StageAttemptFailRequest
    {
        public string ClientRequestId { get; set; } = string.Empty;
        public string Reason { get; set; } = "fail";
    }

    public sealed class StageReviveAdRequest
    {
        public string Provider { get; set; } = string.Empty;
        public string AdToken { get; set; } = string.Empty;
    }
}
