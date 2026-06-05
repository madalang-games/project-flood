using StackExchange.Redis;

namespace ProjectFlood.Application.Rewards;

public sealed class AdMobSsvVerifier : IAdRewardVerifier
{
    private readonly IDatabase _redis;

    public AdMobSsvVerifier(IConnectionMultiplexer redis)
        => _redis = redis.GetDatabase();

    public async Task<AdVerifyResult> VerifyAsync(string provider, string adToken, CancellationToken ct)
    {
        var txId = await _redis.StringGetDeleteAsync($"ssv:{adToken}");
        return txId.HasValue
            ? new AdVerifyResult(true, txId.ToString())
            : new AdVerifyResult(false, string.Empty);
    }
}
