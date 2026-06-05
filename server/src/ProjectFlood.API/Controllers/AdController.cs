using Microsoft.AspNetCore.Mvc;
using ProjectFlood.Application.Stage;
using ProjectFlood.Contracts.Ad;

namespace ProjectFlood.API.Controllers;

[ApiController]
[Route("api/ad")]
public sealed class AdController : ControllerBaseEx
{
    private readonly AdInterstitialService _interstitial;
    private readonly AdDoubleRewardService _doubleReward;

    public AdController(AdInterstitialService interstitial, AdDoubleRewardService doubleReward)
    {
        _interstitial = interstitial;
        _doubleReward = doubleReward;
    }

    [HttpGet("eligibility")]
    public Task<AdEligibilityResponse> GetEligibility(CancellationToken ct)
        => _interstitial.GetEligibilityAsync(PlayerId, ct);

    [HttpPost("interstitial/shown")]
    public Task<AdInterstitialShownResponse> InterstitialShown([FromBody] AdInterstitialShownRequest request, CancellationToken ct)
        => _interstitial.RecordShownAsync(PlayerId, request.StageId, CorrelationId, ct);

    [HttpPost("double-reward/claim")]
    public Task<AdDoubleRewardGrantResponse> ClaimDoubleReward([FromBody] AdDoubleRewardRequest request, CancellationToken ct)
        => _doubleReward.ClaimAsync(PlayerId, request, CorrelationId, ct);
}
