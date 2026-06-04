using ProjectFlood.Application.Common;
using ProjectFlood.Application.Stamina;
using ProjectFlood.Contracts.Rewards;

namespace ProjectFlood.Application.Rewards;

public sealed class AdRewardService
{
    private readonly StaminaService _stamina;

    public AdRewardService(StaminaService stamina)
    {
        _stamina = stamina;
    }

    public async Task<AdRewardClaimResponse> ClaimAsync(long userId, AdRewardClaimRequest request, string correlationId, CancellationToken ct)
    {
        if (request.PlacementId == "STAMINA_LIFE")
        {
            var result = await _stamina.GrantAdLifeAsync(userId, request.Provider, request.ProviderTransactionId, request.AdToken, correlationId, ct);
            return new AdRewardClaimResponse
            {
                Granted = result.Granted,
                Duplicate = result.Duplicate,
                PlacementId = request.PlacementId,
                GrantedRewards = result.Granted
                    ? new List<GrantedRewardDto>
                    {
                        new()
                        {
                            RewardType = "STAMINA_LIFE",
                            TargetId = 0,
                            Amount = result.Delta,
                            DurationSeconds = 0,
                        },
                    }
                    : new List<GrantedRewardDto>(),
                Stamina = result.Stamina,
                ServerTime = result.ServerTime,
            };
        }

        throw new GameApiException("AD_PLACEMENT_NOT_SUPPORTED", "Ad placement is not supported by the generic ad reward API.");
    }
}
