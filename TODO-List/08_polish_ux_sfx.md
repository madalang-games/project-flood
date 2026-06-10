# 비주얼 폴리싱, UX 및 오디오 시스템 체크리스트

고품질의 하이퍼 캐주얼 게임 감성을 전달하기 위해 고급 미학, 마이크로 애니메이션, 화면 전환, 파티클 효과, 폰트 현지화 최적화 및 사운드 디자인(SFX/BGM)을 위한 체크리스트입니다.

## 1. 비주얼 프레젠테이션 및 전환 (MVP)
- [x] **세이프 에리어(Safe Area) 조정**: 모바일 화면 노치나 홈 바(Home indicator)를 피하기 위해 UI 범위를 자동으로 조절합니다.
  - 참조: [SafeAreaHandler.cs](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/client/project-flood/Assets/Scripts/Core/SafeAreaHandler.cs)
- [x] **씬 전환 효과**: Boot, Lobby, InGame 씬 간의 SlideUp, SlideDown, Fade 전환 효과입니다.
  - 참조: [SceneTransition.cs](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/client/project-flood/Assets/Scripts/Core/SafeAreaHandler.cs)
- [x] **부드러운 중력 낙하 애니메이션**: 압착되는 블록들이 즉시 배치되는 대신 시간에 따라 부드럽게 아래로 떨어집니다.
  - 참조: [BoardView.cs:PlayGravity](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/client/project-flood/Assets/Scripts/InGame/View/BoardView.cs)
- [/] **파티클 VFX 피드백**: 폭탄 폭발 효과, 로켓의 궤적 스윕, 기본 셀 제거 시의 일치하는 스프라이트 팝 시퀀스입니다.
  - 상태: [BoardView.cs:PlayRemovalEffects](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/client/project-flood/Assets/Scripts/InGame/View/BoardView.cs)에 기본 파티클이 구현되어 있습니다. 표준 프리미엄 하이퍼 캐주얼 게임(예: Voodoo 또는 SayGames) 수준의 추가적인 비주얼 플래시와 역동적인 폭발 파티클이 필요합니다.

## 2. 현지화 및 폰트 최적화 (MVP)
- [x] **폰트 서브셋(Subset) 툴**: 15개 지원 언어에 대해 콤팩트한 폰트 서브셋을 구축하여 텍스처 저장 용량과 빌드 크기를 최적화하는 자동화 스크립트입니다.
  - 참조: [subset_fonts.bat](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/tools/subset_fonts.bat) 및 [subset_fonts.js](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/tools/subset_tool/subset_fonts.js)
- [x] **현지화 서비스**: 런타임 중 여러 지역에 대한 문자열 교체를 처리합니다.
  - 참조: [LocalizationService.cs](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/client/project-flood/Assets/Scripts/Services/LocalizationService.cs)

## 3. 쥬스(Juice), 폴리싱 및 오디오 통합 (활성 범위)
- [ ] **사운드 매니저 (SFX/BGM)**: 볼륨 파라미터 조절, 배경음악 트랙 재생, 동적 SFX 클립(터치 팝, 폭탄 폭발, 로켓 이동, 골드 코인 수집, 별 잠금 해제음, 실패 오버레이 차임)을 처리하는 싱글톤 오디오 매니저입니다.
- [ ] **마이크로 애니메이션 (버튼 쥬스)**: 버튼 클릭 시의 스케일 바운스, 호버 광채, 진행 표시줄 슬라이드, 잘못된 이동 시의 흔들림 애니메이션을 추가합니다.
- [ ] **축하 화면 폴리싱**: 스테이지 결과 화면에 흩날리는 꽃가루 파티클, 별 연출 시퀀스 및 골드 보상 카운터 롤링 효과를 추가합니다.
- [ ] **햅틱(Haptic) 피드백**: 인게임 액션과 일치하는 미세한 기기 진동(가벼운 터치, 중간 폭발, 강한 흔들림)을 모바일 플랫폼에서 트리거합니다.
- [ ] **동적 배경**: 스테이지 클리어 진행 상황을 시각적으로 나타내기 위해 배경의 수위가 오르내리는 애니메이션을 구현합니다.
