namespace ProjectFlood.Application.Common;

public static class ErrorCodes
{
    public const string InsufficientStamina = "INSUFFICIENT_STAMINA";
    public const string StaminaFull = "STAMINA_FULL";
    public const string InvalidStageAttempt = "INVALID_STAGE_ATTEMPT";
    public const string StageAttemptExpired = "STAGE_ATTEMPT_EXPIRED";
    public const string ReviveLimitExceeded = "REVIVE_LIMIT_EXCEEDED";
    public const string RewardAlreadyClaimed = "REWARD_ALREADY_CLAIMED";
    public const string AdRewardDuplicate = "AD_REWARD_DUPLICATE";
    public const string AdRewardVerifyFailed = "AD_REWARD_VERIFY_FAILED";
    public const string AdSsvPending = "AD_SSV_PENDING";
    public const string DoubleRewardNotEligible = "DOUBLE_REWARD_NOT_ELIGIBLE";
}
