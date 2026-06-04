namespace ProjectFlood.Application.Rewards;

public sealed class DevelopmentAdRewardVerifier : IAdRewardVerifier
{
    public Task<bool> VerifyAsync(string provider, string providerTransactionId, string adToken, CancellationToken ct)
        => Task.FromResult(
            !string.IsNullOrWhiteSpace(provider)
            && !string.IsNullOrWhiteSpace(providerTransactionId)
            && !string.IsNullOrWhiteSpace(adToken));
}
