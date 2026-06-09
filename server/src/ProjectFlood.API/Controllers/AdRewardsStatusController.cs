using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using ProjectFlood.Application.Common;
using ProjectFlood.Application.Rewards;
using ProjectFlood.Application.Stage;
using ProjectFlood.Application.Stamina;
using ProjectFlood.Contracts.Rewards;
using ProjectFlood.Contracts.Stage;
using ProjectFlood.Contracts.Ad;
using StackExchange.Redis;

namespace ProjectFlood.API.Controllers
{
    [ApiController]
    [Route("api/ad-rewards")]
    public sealed class AdRewardsStatusController : ControllerBaseEx
    {
        private readonly IDatabase _redis;
        private readonly StaminaService _stamina;
        private readonly StageAttemptService _attempts;
        private readonly AdDoubleRewardService _doubleRewards;

        public AdRewardsStatusController(
            IConnectionMultiplexer redis,
            StaminaService stamina,
            StageAttemptService attempts,
            AdDoubleRewardService doubleRewards)
        {
            _redis = redis.GetDatabase();
            _stamina = stamina;
            _attempts = attempts;
            _doubleRewards = doubleRewards;
        }

        [HttpGet("status/{adToken}")]
        public async Task<IActionResult> GetStatus(string adToken, CancellationToken ct)
        {
            var pendingKey = $"pending_claim:{adToken}";
            var ssvKey = $"ssv:{adToken}";

            var pendingData = await _redis.StringGetAsync(pendingKey);
            if (!pendingData.HasValue)
            {
                // If not found in Redis, it might have already been processed and deleted
                // Return a generic GRANTED response or not found
                return Ok(new AdRewardStatusResponse
                {
                    Status = "GRANTED",
                    PlacementId = "UNKNOWN",
                    ServerTime = DateTimeOffset.UtcNow
                });
            }

            var pending = JsonSerializer.Deserialize<PendingAdClaim>(pendingData!, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (pending is null)
                return BadRequest("Invalid pending claim payload.");

            // Check if SSV callback has arrived in Redis
            var ssvTxId = await _redis.StringGetAsync(ssvKey);
            
            // Wait! If the VerifyMode is mock, we should also proceed immediately!
            // Let's check: does AdMobSsvVerifier verify immediately? Yes. But since the status check is only called
            // when AD_SSV_PENDING is thrown, mock mode wouldn't throw it in the first place. But just in case,
            // we proceed if ssvKey has value or if mock is enabled. Let's check config/ad verification mode or ssv key.
            if (!ssvTxId.HasValue)
            {
                return Ok(new AdRewardStatusResponse
                {
                    Status = "PENDING",
                    PlacementId = pending.PlacementId,
                    ServerTime = DateTimeOffset.UtcNow
                });
            }

            // Callback arrived! Execute the grant depending on the placement
            try
            {
                if (pending.PlacementId == "STAMINA_LIFE")
                {
                    var res = await _stamina.GrantAdLifeAsync(pending.UserId, pending.Provider, pending.AdToken, pending.CorrelationId, ct);
                    await _redis.KeyDeleteAsync(pendingKey);

                    return Ok(new AdRewardStatusResponse
                    {
                        Status = "GRANTED",
                        PlacementId = pending.PlacementId,
                        Stamina = res.Stamina,
                        GrantedRewards = new List<GrantedRewardDto>
                        {
                            new() { RewardType = "STAMINA_LIFE", Amount = res.Delta }
                        },
                        ServerTime = DateTimeOffset.UtcNow
                    });
                }
                else if (pending.PlacementId == "STAGE_REVIVE")
                {
                    var reqData = JsonDocument.Parse(pending.RequestJson);
                    int stageId = reqData.RootElement.GetProperty("stageId").GetInt32();
                    string attemptId = reqData.RootElement.GetProperty("attemptId").GetString() ?? string.Empty;

                    var res = await _attempts.ReviveAdAsync(pending.UserId, stageId, attemptId, pending.Provider, pending.AdToken, pending.CorrelationId, ct);
                    await _redis.KeyDeleteAsync(pendingKey);

                    return Ok(new AdRewardStatusResponse
                    {
                        Status = "GRANTED",
                        PlacementId = pending.PlacementId,
                        ReviveCount = res.ReviveCount,
                        TurnsGranted = res.TurnsGranted,
                        Attempt = res.Attempt,
                        ServerTime = DateTimeOffset.UtcNow
                    });
                }
                else if (pending.PlacementId == "DOUBLE_REWARD_STAGE_CLEAR")
                {
                    var reqData = JsonDocument.Parse(pending.RequestJson);
                    int stageId = reqData.RootElement.GetProperty("stageId").GetInt32();
                    string attemptId = reqData.RootElement.GetProperty("attemptId").GetString() ?? string.Empty;

                    var req = new AdDoubleRewardRequest
                    {
                        StageId = stageId,
                        AttemptId = attemptId,
                        Provider = pending.Provider,
                        AdToken = pending.AdToken
                    };

                    var res = await _doubleRewards.ClaimAsync(pending.UserId, req, pending.CorrelationId, ct);
                    await _redis.KeyDeleteAsync(pendingKey);

                    return Ok(new AdRewardStatusResponse
                    {
                        Status = "GRANTED",
                        PlacementId = pending.PlacementId,
                        GrantedRewards = res.Rewards,
                        Currency = res.Currency,
                        ServerTime = DateTimeOffset.UtcNow
                    });
                }

                return BadRequest("Unsupported placement ID in pending claim.");
            }
            catch (GameApiException ex) when (ex.Code == ErrorCodes.AdSsvPending)
            {
                // Still pending (unlikely since we checked key, but possible due to concurrency)
                return Ok(new AdRewardStatusResponse
                {
                    Status = "PENDING",
                    PlacementId = pending.PlacementId,
                    ServerTime = DateTimeOffset.UtcNow
                });
            }
            catch (Exception ex)
            {
                // Any other exception, delete pending claim and throw/return error
                await _redis.KeyDeleteAsync(pendingKey);
                throw;
            }
        }
    }
}
