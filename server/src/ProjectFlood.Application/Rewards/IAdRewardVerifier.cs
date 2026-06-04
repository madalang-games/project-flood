namespace ProjectFlood.Application.Rewards;

public interface IAdRewardVerifier
{
    Task<bool> VerifyAsync(string provider, string providerTransactionId, string adToken, CancellationToken ct);
}
