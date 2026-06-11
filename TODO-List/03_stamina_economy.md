# 스테미나 및 경제 시스템 체크리스트

스테미나 생명 게이트, 광고-스테미나 보상, 골드 획득/소비 및 서버 측 상태 동기화를 위한 체크리스트입니다.

## 1. 스테미나 생명 및 재생 규칙 (MVP)
- [x] **최대 생명 제한**: 최대 5개의 생명 용량을 가집니다.
- [x] **자연 회복**: 최대 생명 미만일 때 600초(10분)마다 생명 1개가 재생됩니다.
  - 참조: [StaminaService.cs:L174](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/server/src/ProjectFlood.Application/Stamina/StaminaService.cs#L174)
- [x] **스테이지 시작 생명 소모**: 무한 스테미나가 활성화되지 않은 경우, 스테이지 도전을 시작할 때 생명 1개를 소모합니다.
  - 참조: [StaminaService.cs:L110](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/server/src/ProjectFlood.Application/Stamina/StaminaService.cs#L110)
- [x] **스테이지 클리어 생명 환불**: 스테이지 클리어 성공 시(별 1개 이상) 소모된 생명을 환불합니다(최대 생명 한도 내).
  - 참조: [StaminaService.cs:L127](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/server/src/ProjectFlood.Application/Stamina/StaminaService.cs#L127)
- [x] **광고 보상형 생명 지급**: 광고 지급 엔드포인트를 통해 생명을 요청하면 생명 1개를 추가합니다(최대 생명 상태인 경우 거부됨).
  - 참조: [StaminaService.cs:L37](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/server/src/ProjectFlood.Application/Stamina/StaminaService.cs#L37)
- [x] **무한 스테미나**: 일정 시간 동안 무한 플레이가 가능합니다(중첩 정책 `EXTEND`). 무한 스테미나 기간 중에도 자연 회복은 계속됩니다.
  - 참조: [StaminaService.cs:L142](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/server/src/ProjectFlood.Application/Stamina/StaminaService.cs#L142)
- [x] **StaminaPopupView**: 큰 하트, 개수, 타이머/MAX 라벨, 광고 보기(+1) 버튼(최대 생명 시 비활성화)이 포함된 스테미나 팝업입니다.
  - 참조: [StaminaPopupView.cs](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/client/project-flood/Assets/Scripts/OutGame/Lobby/StaminaPopupView.cs)
- [x] **스테미나 클라이언트 UI 및 API 훅**: HeaderView에 스테미나 개수와 재생 타이머를 표시합니다. 스테미나 패널 터치 시 StaminaPopupView를 엽니다. StageInfoPopupView는 스테이지 입장 전 스테미나를 체크합니다. StaminaApiService가 UI와 연결되었습니다.
  - 상태: 완료. 로비 로딩 시 `LobbyView.Start()`에서 `StaminaApiService.FetchStamina()`를 호출합니다. `HeaderView.Update()`는 매 프레임 예상 생명을 폴링합니다. `StageInfoPopupView.OnPlay`는 `GetEstimatedLife()`를 통해 입장을 제어합니다.
  - 참조: [HeaderView.cs](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/client/project-flood/Assets/Scripts/OutGame/Lobby/HeaderView.cs), [StageInfoPopupView.cs](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/client/project-flood/Assets/Scripts/OutGame/Lobby/StageInfoPopupView.cs), [StaminaApiService.cs](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/client/project-flood/Assets/Scripts/Services/StaminaApiService.cs)

## 2. 골드 경제 (MVP)
- [x] **스테이지 클리어 골드 보상**: 성적에 따라 골드를 지급합니다: `기본보상(별 개수) + (남은 턴 수 * 5)`.
  - 참조: [InGameSceneEntry.cs:L190](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/client/project-flood/Assets/Scripts/InGame/Controller/InGameSceneEntry.cs#L190)
- [x] **이어하기 골드 소모(Sink)**: 턴 소모 시 플레이어에게 150 골드를 사용하여 +3 턴을 얻도록 제안합니다(시도당 1회).
  - 참조: [InGameSceneEntry.cs:L113](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/client/project-flood/Assets/Scripts/InGame/Controller/InGameSceneEntry.cs#L113)
- [x] **골드 서버 동기화**: 로컬 PlayerPrefs 골드 추적에서 서버 동기화 골드 통화 API로 전환합니다.
  - 상태: 완료. `FetchGold` (GET `/api/currency`) 및 `SpendGold` (POST `/api/currency/spend`)를 포함한 `CurrencyApiService`가 추가되었습니다. 로비 로딩 시 골드를 가져옵니다. 스테이지 클리어 시 `StageAttemptEndResponse.Currency`에서 동기화합니다. 이어하기 소모는 `SpendGold`를 통해 서버 측에서 차감됩니다. 서버 응답에 따라 `PlayerProgressService.SetGold`가 적용됩니다.

## 3. 보상 수령 (MVP)
- [x] **비광고형 보상 수령**: 보상 그룹을 수령하기 위한 공통 API 엔드포인트 `/api/rewards/claim`.
  - 참조: [RewardsController.cs](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/server/src/ProjectFlood.API/Controllers/RewardsController.cs)
- [x] **수령 제한 기간**: 보상에 대해 일일/시간당 수령 제한을 적용합니다(`user_reward_claim_state` 테이블에 진행 상황 저장).
  - 참조: [StaminaService.cs의 EnsureStateAsync](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/server/src/ProjectFlood.Application/Stamina/StaminaService.cs#L156)
- [/] **HomeTab 수령 배지 UI**: 챕터 마일스톤 상자 및 일일 무료 아이템(예: 일일 골드 상자)을 표시하는 인게임 HUD 또는 로비 팝업.
  - [x] 활성 보상 소스 가용성 확인을 위한 클라이언트 체크 `/api/rewards/sources` 구현 및 `HomeTabView` 연동 완료. (완료)
  - [x] 챕터 마일스톤 상자 구현: 별 3개 조건 체크 및 서버 보상 수령 연동 완료. (완료)
  - [ ] 홈 탭 일일 무료 버튼(Daily Free Box)에 알림 배지를 렌더링하고, 클릭 시 팝업을 표시합니다. (진행 예정)

## 4. 제외 범위 (Phase 2+)
- [ ] **부스터 상점 UI**: 플레이어가 골드를 사용하여 아이템 번들을 구매할 수 있는 로비 내 UI 패널(폭탄 1개 = 100 골드, 로켓 1개 = 80 골드). (사용자 요청에 따라 제외)
- [ ] **스테미나 상점 / 구매**: 실제 결제(IAP 통합) 또는 높은 골드 가격으로 무한 스테미나 기간을 직접 구매하는 상점 옵션. (사용자 요청에 따라 제외)
- [ ] **동적 재생 수정자**: 스테미나 회복 속도를 높여주는 일시적 아이템 또는 구독 보너스(예: 600초 대신 300초당 생명 1개). (사용자 요청에 따라 제외)
