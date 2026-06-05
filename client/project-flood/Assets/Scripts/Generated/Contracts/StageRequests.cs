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
        public int Score { get; set; }
        public int RemainingTurns { get; set; }
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
