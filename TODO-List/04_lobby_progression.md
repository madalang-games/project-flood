# 로비, 스테이지 진행 및 메타 시스템 체크리스트

메인 로비 화면, 스테이지 로드맵 뷰, 진행 메커니즘, 소셜/랭킹 리더보드 통합을 위한 체크리스트입니다.

## 1. 메인 로비 및 하단 네비게이션 (MVP)
- [x] **하단 네비게이션 탭**: 네비게이션용 탭(홈, 랭킹, 설정)입니다.
  - 참조: [BottomNavBarView.cs](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/client/project-flood/Assets/Scripts/OutGame/Lobby/BottomNavBarView.cs)
- [x] **로비 뷰 관리**: 패널 전환 및 탭 새로고침 트리거입니다.
  - 참조: [LobbyView.cs](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/client/project-flood/Assets/Scripts/OutGame/Lobby/LobbyView.cs)
- [x] **상단 헤더 정보**: 플레이어 닉네임/아바타와 현재 보유 재화(골드)를 표시합니다.
  - 참조: [HeaderView.cs](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/client/project-flood/Assets/Scripts/OutGame/Lobby/HeaderView.cs)

## 2. 스테이지 진행 및 로드맵 (MVP)
- [x] **S자형 스테이지 로드맵**: 세로 스크롤 지그재그 형식으로 스테이지를 동적으로 렌더링하며 Catmull-Rom 곡선 커넥터를 사용합니다.
  - 참조: [HomeTabView.cs](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/client/project-flood/Assets/Scripts/OutGame/Lobby/HomeTabView.cs)
- [x] **스테이지 노드 뷰 상태**: 스테이지별 올바른 비주얼 상태(잠김, 잠금 해제, 현재 위치 표시, 획득한 별 개수 0-3개)를 표시합니다.
  - 참조: [StageNodeView.cs](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/client/project-flood/Assets/Scripts/OutGame/Lobby/StageNodeView.cs)
- [x] **스테이지 선택 정보 팝업**: 플레이 전 스테이지 상세 정보(최고 별 점수, 턴 제한)를 표시합니다.
  - 참조: [StageInfoPopupView.cs](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/client/project-flood/Assets/Scripts/OutGame/Lobby/StageInfoPopupView.cs)
- [x] **로컬 순차 잠금 해제**: 스테이지 N 클리어 후 로컬에서 스테이지 N+1을 잠금 해제합니다.
  - 참조: [PlayerProgressService.cs:L61](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/client/project-flood/Assets/Scripts/Services/PlayerProgressService.cs#L61)

## 3. 소셜 및 랭킹 (MVP)
- [x] **글로벌 랭킹 API**: 총 획득 별 또는 최고 클리어 스테이지에 대한 페이지 단위 리더보드를 가져옵니다. DB 집계로부터 Redis 인덱스를 재구축합니다.
  - 참조: [RankingController.cs](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/server/src/ProjectFlood.API/Controllers/RankingController.cs) 및 [RankingService.cs](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/server/src/ProjectFlood.Application/Ranking/RankingService.cs)
- [x] **랭킹 탭 뷰**: 푸터에 현재 사용자의 프로필 랭킹 카드가 포함된 페이지 단위 글로벌 리더보드(별 개수, 최고 스테이지)입니다.
  - 참조: [RankingTabView.cs](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/client/project-flood/Assets/Scripts/OutGame/Lobby/RankingTabView.cs)
- [x] **스테이지별 랭킹**: 스테이지당 최고 턴 랭킹(경쟁 스타일: 내 기록 vs 타인 기록). Redis 데이터 손실 시 재구축됩니다.
  - 참조: [StageAttemptService.cs:L109](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/server/src/ProjectFlood.Application/Stage/StageAttemptService.cs#L109)

- [x] **챕터 그룹화 및 비주얼 테마**: 스테이지를 챕터로 그룹화합니다. 쉐이더 기반 배경 교체와 `theme_key`를 사용하여 환경 스프라이트/테마를 업데이트합니다. (완료)

## 4. 로비 및 진행 확장 (활성 범위)
- [x] **챕터 마일스톤 상자**: 각 챕터 끝부분의 로비 로드맵에 상자를 렌더링합니다. 챕터 내 모든 스테이지에서 별 3개를 획득한 경우에만 잠금 해제 및 수령(1회) 가능합니다.
  - [x] 보상 수령 시 서버 검증 구현: `user_stage_progress`의 별 3개 클리어 여부를 체크합니다.
- [x] **플레이어 커스텀 프로필**: 플레이어가 표시 이름을 변경하고, 골드나 업적을 통해 고유한 프로필 아바타를 선택/잠금 해제할 수 있게 합니다.
  - [x] 백엔드 프로필 업데이트 API 및 활성 아바타 ID용 DB 컬럼 구현.
- [x] **동적 랭킹 리스트 가상화**: 수천 개의 항목이 포함된 리스트를 효율적으로 처리하기 위해 스크롤 가상화(virtualization)로 Unity 랭킹 UI를 최적화합니다.
