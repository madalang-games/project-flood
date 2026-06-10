# 광고 및 수익화 시스템 체크리스트

보상형 광고, 전면 광고 표시 쿨타임, 보상 두 배, AdMob SDK 및 IAP 수익화를 위한 체크리스트입니다.

## 1. AdMob SDK 및 지점(Placements) (MVP)
- [x] **AdMob Unity SDK 통합**: 스테미나 생명(STAMINA_LIFE), 스테이지 부활(STAGE_REVIVE), 스테이지 클리어 보상 두 배(DOUBLE_REWARD_STAGE_CLEAR)를 위한 테스트 지점이 포함된 핵심 SDK 설정입니다.
  - 참조: [AdMobService.cs](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/client/project-flood/Assets/Scripts/Services/AdMobService.cs)
- [x] **보상형 광고 요청 및 검증**: 커스텀 검증 nonce(SSV 커스텀 데이터)와 함께 보상형 광고를 요청하고 성공 콜백을 검증합니다.
  - 상태: 클라이언트가 [AdMobService.cs:L75](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/client/project-flood/Assets/Scripts/Services/AdMobService.cs#L75)에서 nonce를 요청합니다. 서버는 [AdSsvCallbackController.cs](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/server/src/ProjectFlood.API/Controllers/AdSsvCallbackController.cs)에서 콜백을 수신하고 `ad_reward_transactions` 테이블에서 트랜잭션을 추적합니다. 클라이언트는 검증 지연 시간 동안 로딩 차단 오버레이를 처리합니다.

## 2. 인게임 광고 지점 (MVP)
- [x] **스테미나 광고 지점**: 생명 개수가 최대치 미만일 때 광고를 시청하면 생명 +1을 지급합니다.
  - 참조: [StaminaService.cs:L37](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/server/src/ProjectFlood.Application/Stamina/StaminaService.cs#L37)
- [x] **스테이지 부활 광고 지점**: 턴 소모 시 보상형 광고를 시청하여 추가 턴을 얻고 부활합니다(1차 부활 = +3턴, 2차 부활 = +2턴, 3차 부활 = +1턴).
  - 참조: [StageAttemptService.cs:L152](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/server/src/ProjectFlood.Application/Stage/StageAttemptService.cs#L152)

## 3. 게임 종료 후 및 쿨타임 광고 (MVP)
- [x] **스테이지 종료 후 전면 광고**: 서버의 자격 확인에 따라 스테이지 클리어 시 전면 광고를 표시합니다.
  - 참조: [AdInterstitialService.cs](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/server/src/ProjectFlood.Application/Stage/AdInterstitialService.cs)
- [x] **전면 광고 쿨타임 체크**: `user_interstitial_state` 테이블을 확인하여 쿨타임 로직(광고 간 시간 임계값, 최소 클리어 스테이지 번호, 일일 최대 제한)을 강제합니다.
  - 참조: [AdInterstitialService.cs:L44](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/server/src/ProjectFlood.Application/Stage/AdInterstitialService.cs#L44)
- [x] **스테이지 클리어 보상 두 배**: 결과 화면에서 보상형 광고를 시청하여 클리어 보상인 골드/아이템을 두 배로 늘리는 옵션입니다.
  - 참조: [AdDoubleRewardService.cs](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/server/src/ProjectFlood.Application/Stage/AdDoubleRewardService.cs)
- [x] **광고 자격 클라이언트 캐시**: 전면 광고 표시를 요청하기 전 자격 변수를 확인하기 위한 클라이언트 측 로컬 캐시입니다.
  - 참조: [AdEligibilityCache.cs](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/client/project-flood/Assets/Scripts/Services/AdEligibilityCache.cs)

## 4. 광고 검증 (활성 범위)
- [x] **보상형 광고 검증 UX**: 광고 종료 후 SSV 콜백이 처리되는 동안 로딩 차단 오버레이(`LoadingOverlayView.prefab`)를 표시합니다. 1초마다 `/api/ad-rewards/status/{txId}`를 폴링(최대 10초)하여 성급하게 닫히는 것을 방지합니다.
- [x] **AdMob 쿨타임 강제**: 클릭 스팸을 방지하고 AdMob의 무효 트래픽 정책을 준수하기 위해 광고 버튼에 30초의 클라이언트 측 쿨타임을 적용합니다.

## 5. 제외 범위 (Phase 2+)
- [ ] **IAP "광고 제거" 구매**: 사용자가 "광고 제거" 패키지를 구매할 수 있도록 인앱 결제(Unity IAP)를 통합합니다. 구매 시 강제 전면 광고는 표시되지 않습니다(보상형 광고는 유지). (사용자 요청에 따라 제외)
- [ ] **미디에이션(Mediation) 통합**: 네트워크 전반에 걸쳐 광고 충전율(Fill rate)과 eCPM 수익을 최적화하기 위해 AppLovin MAX 또는 Unity Ads 미디에이션을 연결합니다. (사용자 요청에 따라 제외)
- [ ] **동적 광고 지점 설정**: 고정된 파일이 아닌 동적 서버 설정을 통해 광고 지점 변수(쿨타임 시간, 최소 레벨 등)를 가져옵니다. (사용자 요청에 따라 제외)
