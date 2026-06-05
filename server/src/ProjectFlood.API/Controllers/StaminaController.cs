using Microsoft.AspNetCore.Mvc;
using ProjectFlood.Application.Stamina;
using ProjectFlood.Contracts.Stamina;

namespace ProjectFlood.API.Controllers;

[ApiController]
[Route("api/stamina")]
public sealed class StaminaController : ControllerBaseEx
{
    private readonly StaminaService _stamina;

    public StaminaController(StaminaService stamina)
    {
        _stamina = stamina;
    }

    [HttpGet]
    public Task<StaminaStatusResponse> Get(CancellationToken ct)
        => _stamina.GetAsync(PlayerId, ct);

    [HttpPost("ad-life-reward")]
    public Task<StaminaAdLifeRewardResponse> AdLifeReward([FromBody] StaminaAdLifeRewardRequest request, CancellationToken ct)
        => _stamina.GrantAdLifeAsync(PlayerId, request.Provider, request.AdToken, CorrelationId, ct);
}
