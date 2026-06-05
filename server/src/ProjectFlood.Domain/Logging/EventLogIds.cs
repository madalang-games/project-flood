namespace ProjectFlood.Domain.Logging;

public static class EventLogIds
{
    public const int StaminaLifeChanged = 2001;
    public const int StaminaUnlimitedChanged = 2002;

    public const int StageAttemptStarted = 2101;
    public const int StageAttemptCleared = 2102;
    public const int StageAttemptFailed = 2103;
    public const int StageAttemptReplaced = 2104;
    public const int StageAttemptRevivedByAd = 2105;
    public const int StageClearRewardGranted = 2106;

    public const int CurrencyChanged = 3001;

    public const int RewardClaimed = 6001;
    public const int AdRewardClaimed = 6101;
    public const int AdInterstitialShown = 6102;
    public const int AdDoubleRewardGranted = 6103;
}
