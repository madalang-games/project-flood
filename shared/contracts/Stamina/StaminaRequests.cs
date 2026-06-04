#nullable enable
namespace ProjectFlood.Contracts.Stamina
{
    public sealed class StaminaAdLifeRewardRequest
    {
        public string Provider { get; set; } = string.Empty;
        public string ProviderTransactionId { get; set; } = string.Empty;
        public string AdToken { get; set; } = string.Empty;
    }
}
