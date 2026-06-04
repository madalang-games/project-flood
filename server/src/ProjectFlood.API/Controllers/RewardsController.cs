using Microsoft.AspNetCore.Mvc;
using ProjectFlood.Application.Rewards;
using ProjectFlood.Contracts.Rewards;

namespace ProjectFlood.API.Controllers;

[ApiController]
[Route("api/rewards")]
public sealed class RewardsController : ControllerBaseEx
{
    private readonly RewardService _rewards;

    public RewardsController(RewardService rewards)
    {
        _rewards = rewards;
    }

    [HttpGet("sources")]
    public Task<RewardSourcesResponse> Sources(CancellationToken ct)
        => _rewards.GetSourcesAsync(PlayerId, ct);

    [HttpPost("claim")]
    public Task<RewardClaimResponse> Claim([FromBody] RewardClaimRequest request, CancellationToken ct)
        => _rewards.ClaimAsync(PlayerId, request.SourceId, CorrelationId, ct);
}
