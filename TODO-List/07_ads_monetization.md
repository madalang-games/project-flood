# Ads & Monetization System Checklist

Checklist for rewarded ads, interstitial ad display cooldowns, double rewards, AdMob SDK, and IAP monetization.

## 1. AdMob SDK & Placements (MVP)
- [x] **AdMob Unity SDK Integration**: Core SDK setup with test placements for STAMINA_LIFE, STAGE_REVIVE, and DOUBLE_REWARD_STAGE_CLEAR.
  - Reference: [AdMobService.cs](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/client/project-flood/Assets/Scripts/Services/AdMobService.cs)
- [x] **Rewarded Ad Request & Verify**: Request rewarded ads with custom verification nonce (SSV custom data), validating success callbacks.
  - Status: Client requests nonce in [AdMobService.cs:L75](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/client/project-flood/Assets/Scripts/Services/AdMobService.cs#L75). Server receives callback in [AdSsvCallbackController.cs](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/server/src/ProjectFlood.API/Controllers/AdSsvCallbackController.cs) and tracks transactions in `ad_reward_transactions` table. Client handles loading blocker overlay during verify delay.

## 2. In-Game Ad Placements (MVP)
- [x] **Stamina Ad Placement**: Spend ad view to grant +1 life when life count is less than Max Life.
  - Reference: [StaminaService.cs:L37](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/server/src/ProjectFlood.Application/Stamina/StaminaService.cs#L37)
- [x] **Stage Revive Ad Placement**: On turn exhaustion, watch rewarded ad to revive with extra turns (1st revive = +3 turns, 2nd revive = +2 turns, 3rd revive = +1 turn).
  - Reference: [StageAttemptService.cs:L152](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/server/src/ProjectFlood.Application/Stage/StageAttemptService.cs#L152)

## 3. Post-Game & Cooldown Ads (MVP)
- [x] **Post-Stage Interstitial Ads**: Displays interstitial ads on stage clear according to server eligibility.
  - Reference: [AdInterstitialService.cs](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/server/src/ProjectFlood.Application/Stage/AdInterstitialService.cs)
- [x] **Interstitial Cooldown Checking**: Enforces cooldown logic (time threshold between ads, min clear stage number, daily max limits) checking `user_interstitial_state` table.
  - Reference: [AdInterstitialService.cs:L44](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/server/src/ProjectFlood.Application/Stage/AdInterstitialService.cs#L44)
- [x] **Double Stage-Clear Rewards**: Option to watch rewarded ad on result screen to double gold/item clear rewards.
  - Reference: [AdDoubleRewardService.cs](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/server/src/ProjectFlood.Application/Stage/AdDoubleRewardService.cs)
- [x] **Ad Eligibility Client Cache**: Client-side local cache to check eligibility variables before requesting interstitial displays.
  - Reference: [AdEligibilityCache.cs](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/client/project-flood/Assets/Scripts/Services/AdEligibilityCache.cs)

## 4. Ads Verification (Active Scope)
- [x] **Rewarded Ad Verify UX**: Show loading blocker overlay (`LoadingOverlayView.prefab`) while SSV callback settles after ad closes; prevent premature dismiss by polling `/api/ad-rewards/status/{txId}` every 1s (max 10s).
- [x] **AdMob Cooldown enforcement**: Enforce 30-second client-side cooldown on the ad buttons to prevent click spamming and comply with AdMob invalid traffic policies.

## 5. Excluded Scope (Phase 2+)
- [ ] **IAP "No-Ads" Purchase**: Integrated In-App Purchases (Unity IAP) allowing users to purchase "No-Ads" package. If purchased, suppress forced interstitial ads (rewarded ads remain active). (Excluded per user request)
- [ ] **Mediation Integration**: Wire AppLovin MAX or Unity Ads Mediation to optimize ad fill rates and eCPM returns across networks. (Excluded per user request)
- [ ] **Dynamic Ad Placement Configs**: Fetch ad placement variables (cooldown durations, min levels) from dynamic server configuration rather than static files. (Excluded per user request)
