using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ProjectFlood.Application.Stage;
using ProjectFlood.Contracts.Stage;

namespace ProjectFlood.API.Controllers;

[ApiController]
[Route("api/stages")]
public sealed class StageController : ControllerBaseEx
{
    private readonly StageAttemptService _attempts;

    public StageController(StageAttemptService attempts)
    {
        _attempts = attempts;
    }

    [HttpPost("{stageId:int}/attempts/start")]
    [EnableRateLimiting("stage_start")]
    public Task<StageAttemptStartResponse> Start(int stageId, [FromBody] StageAttemptStartRequest request, CancellationToken ct)
        => _attempts.StartAsync(PlayerId, stageId, CorrelationId, ct);

    [HttpPost("{stageId:int}/attempts/{attemptId}/clear")]
    public Task<StageAttemptEndResponse> Clear(int stageId, string attemptId, [FromBody] StageAttemptClearRequest request, CancellationToken ct)
        => _attempts.ClearAsync(PlayerId, stageId, attemptId, CorrelationId, ct);

    [HttpPost("{stageId:int}/attempts/{attemptId}/fail")]
    public Task<StageAttemptEndResponse> Fail(int stageId, string attemptId, [FromBody] StageAttemptFailRequest request, CancellationToken ct)
        => _attempts.FailAsync(PlayerId, stageId, attemptId, request.Reason, CorrelationId, ct);

    [HttpPost("{stageId:int}/attempts/{attemptId}/revive-ad")]
    public Task<StageReviveAdResponse> ReviveAd(int stageId, string attemptId, [FromBody] StageReviveAdRequest request, CancellationToken ct)
        => _attempts.ReviveAdAsync(PlayerId, stageId, attemptId, request.Provider, request.ProviderTransactionId, request.AdToken, CorrelationId, ct);
}
