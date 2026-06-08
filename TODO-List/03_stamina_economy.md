# Stamina & Economy System Checklist

Checklist for stamina life gates, ad-stamina rewards, gold acquisition/consumption, and server-side state sync.

## 1. Stamina Life & Regen Rules (MVP)
- [x] **Max Life Limit**: Maximum of 5 lives capacity.
- [x] **Natural Recovery**: 1 life regenerates every 600 seconds (10 minutes) when below Max Life.
  - Reference: [StaminaService.cs:L174](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/server/src/ProjectFlood.Application/Stamina/StaminaService.cs#L174)
- [x] **Stage Start Life Spend**: Starting a stage attempt consumes 1 life, unless Unlimited Stamina is active.
  - Reference: [StaminaService.cs:L110](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/server/src/ProjectFlood.Application/Stamina/StaminaService.cs#L110)
- [x] **Stage Clear Life Refund**: Clearing the stage successfully (>= 1 Star) refunds the spent life (capped at Max Life).
  - Reference: [StaminaService.cs:L127](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/server/src/ProjectFlood.Application/Stamina/StaminaService.cs#L127)
- [x] **Ad Rewarded Life Grant**: Claiming life via ad grant endpoint adds 1 life (rejected if at Max Life).
  - Reference: [StaminaService.cs:L37](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/server/src/ProjectFlood.Application/Stamina/StaminaService.cs#L37)
- [x] **Unlimited Stamina**: Duration-based unlimited play time (stack policy `EXTEND`). Natural recovery continues during unlimited period.
  - Reference: [StaminaService.cs:L142](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/server/src/ProjectFlood.Application/Stamina/StaminaService.cs#L142)
- [x] **StaminaPopupView**: Stamina popup with large heart, count, timer/MAX label, and Watch Ad (+1) button (dimmed at MAX life).
  - Reference: [StaminaPopupView.cs](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/client/project-flood/Assets/Scripts/OutGame/Lobby/StaminaPopupView.cs)
- [x] **Stamina Client UI & API Hook**: HeaderView shows stamina count and regen timer; stamina panel tap opens StaminaPopupView; StageInfoPopupView checks stamina before allowing stage entry. StaminaApiService wired to UI.
  - Status: Done. `LobbyView.Start()` calls `StaminaApiService.FetchStamina()` on lobby load. `HeaderView.Update()` polls estimated life every frame. `StageInfoPopupView.OnPlay` gates entry via `GetEstimatedLife()`.
  - Reference: [HeaderView.cs](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/client/project-flood/Assets/Scripts/OutGame/Lobby/HeaderView.cs), [StageInfoPopupView.cs](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/client/project-flood/Assets/Scripts/OutGame/Lobby/StageInfoPopupView.cs), [StaminaApiService.cs](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/client/project-flood/Assets/Scripts/Services/StaminaApiService.cs)

## 2. Gold Economy (MVP)
- [x] **Stage Clear Gold Reward**: Award gold based on performance: `BaseReward(stars) + (RemainingTurns * 5)`.
  - Reference: [InGameSceneEntry.cs:L190](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/client/project-flood/Assets/Scripts/InGame/Controller/InGameSceneEntry.cs#L190)
- [x] **Continue Economy Sink**: On turn exhaustion, prompt player to spend 150 gold for +3 turns (once per attempt).
  - Reference: [InGameSceneEntry.cs:L113](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/client/project-flood/Assets/Scripts/InGame/Controller/InGameSceneEntry.cs#L113)
- [x] **Gold Server Sync**: Transition from local PlayerPrefs gold tracking to server-synced gold currency APIs.
  - Status: Done. `CurrencyApiService` added with `FetchGold` (GET `/api/currency`) and `SpendGold` (POST `/api/currency/spend`). `LobbyView` fetches gold on load. Stage clear syncs from `StageAttemptEndResponse.Currency`. Continue spend deducts server-side via `SpendGold`. `PlayerProgressService.SetGold` applied on server responses.

## 3. Reward Claims (MVP)
- [x] **Non-Ad Reward Claims**: Common API endpoint `/api/rewards/claim` to claim reward groups.
  - Reference: [RewardsController.cs](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/server/src/ProjectFlood.API/Controllers/RewardsController.cs)
- [x] **Claim Limit Periods**: Enforce daily/hourly claim limits for rewards (stores progress in `user_reward_claim_state` table).
  - Reference: [EnsureStateAsync in StaminaService.cs](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/server/src/ProjectFlood.Application/Stamina/StaminaService.cs#L156)
- [ ] **HomeTab Claim Badge UI**: In-game HUD or Lobby popup showing daily free items (e.g. daily gold chest or free unlimited stamina badge).
  - [ ] Implement client check for active reward source availability `/api/rewards/sources`.
  - [ ] Render notification badge on home tab daily free button, showing popup when clicked.

## 4. Excluded Scope (Phase 2+)
- [ ] **Booster Shop UI**: UI panel inside Lobby allowing players to spend Gold to buy item bundles (1 Bomb = 100 gold, 1 Rocket = 80 gold). (Excluded per user request)
- [ ] **Stamina Shop / Purchases**: Shop options to buy unlimited stamina periods directly via real-money purchases (IAP integration) or high gold values. (Excluded per user request)
- [ ] **Dynamic Regen Modifiers**: Temporary items or subscription bonuses that speed up stamina recovery (e.g., 1 life per 300 seconds instead of 600). (Excluded per user request)
