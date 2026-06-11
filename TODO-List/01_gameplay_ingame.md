# 인게임 및 핵심 게임플레이 규칙 체크리스트

핵심 매치-앤-콜랩스(match-and-collapse) 게임플레이 메커니즘, 규칙 엔진, 보드 기믹, 승리/실패 조건 검증을 위한 체크리스트입니다.

## 1. 핵심 매치 및 중력 규칙 (MVP)
- [x] **BFS 동일 색상 선택**: 터치한 셀의 색상과 일치하는 4방향 인접 셀을 모두 찾습니다. 대각선 인접은 무시됩니다. 그룹 크기가 1인 고립된 셀도 제거 가능합니다.
  - 참조: [GroupSelector.cs](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/client/project-flood/Assets/Scripts/InGame/Rules/GroupSelector.cs)
- [x] **하향 중력 압착**: 공중에 떠 있는 셀은 아래로 떨어집니다. 수평 압착은 없습니다(빈 열은 비어 있는 상태로 유지).
  - 참조: [GravitySystem.cs](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/client/project-flood/Assets/Scripts/InGame/Rules/GravitySystem.cs)
- [x] **Void 경계 중력 분할**: Void 셀은 중력 경계 역할을 합니다. 중력은 Void 경계에 의해 분할된 각 열 세그먼트 내에서 독립적으로 적용됩니다.
  - 참조: [GravitySystem.cs](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/client/project-flood/Assets/Scripts/InGame/Rules/GravitySystem.cs)

## 2. 그리드 및 기믹 셀 (MVP)
- [x] **장애물(Obstacle) 셀**: 선택 및 제거율 계산에서 제외됩니다. 아이템 효과(폭탄, 로켓)로만 파괴할 수 있습니다.
  - 참조: [CellData.cs](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/client/project-flood/Assets/Scripts/InGame/Board/CellData.cs)
- [x] **Void 셀**: 보드 모양 경계(L자형, T자형 등)를 형성합니다. 보이지 않고 상호작용이 불가능하며, 제거율 분모에서 제외됩니다.
  - 참조: [CellData.cs](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/client/project-flood/Assets/Scripts/InGame/Board/CellData.cs)
- [x] **프로텍터(Protector) 셀 (1-2 레이어)**: 직접 타격 제거 규칙(동일 색상 그룹 터치 또는 직접 아이템 적용 시 제거). 기본 셀이 드러날 때까지 강도 레이어를 감소시킵니다.
  - 참조: [ProtectorSystem.cs](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/client/project-flood/Assets/Scripts/InGame/Rules/ProtectorSystem.cs)
- [x] **코어(Core) 셀**: 최종 스테이지 관문입니다. 제거율과 상관없이 스테이지를 클리어하려면 반드시 모두 제거해야 합니다.
  - 참조: [CellData.cs](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/client/project-flood/Assets/Scripts/InGame/Board/CellData.cs)

## 3. 게임 루프 및 컨트롤 (MVP)
- [x] **턴 소모**: 일반 터치는 사용 가능한 턴을 감소시킵니다. 아이템 사용은 턴을 소모하지 않습니다.
  - 참조: [TurnManager.cs](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/client/project-flood/Assets/Scripts/InGame/Controller/TurnManager.cs)
- [x] **180° 보드 회전 기믹**: 보드 중심을 기준으로 180도 회전하고, 논리적 그리드 노드를 교체한 후 중력을 적용합니다. 개발자 UI 버튼을 통해 트리거됩니다.
  - 참조: [InGameController.cs:L119](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/client/project-flood/Assets/Scripts/InGame/Controller/InGameController.cs#L119) 및 [BoardState.cs:L20](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/client/project-flood/Assets/Scripts/InGame/Board/BoardState.cs#L20)
- [x] **승리/실패 판정**: 비율 기반 판정(1성 = 80%, 2성 = 90%, 3성 = 100%/모든 기본 셀 제거). 코어 셀이 남아 있으면 실패합니다.
  - 참조: [ClearEvaluator.cs](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/client/project-flood/Assets/Scripts/InGame/Rules/ClearEvaluator.cs)
- [x] **스테이지 종료 트리거**: 3성 달성(조기 종료) 또는 턴 = 0일 때 자동 종료됩니다.
  - 참조: [InGameController.cs:L148](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/client/project-flood/Assets/Scripts/InGame/Controller/InGameController.cs#L148)

## 4. 인게임 및 기믹 개발 (활성 범위)
- [x] **동적 턴 간격 보드 회전**: 플레이어 턴 N회마다 보드가 자동으로 180도 회전합니다(스테이지 데이터의 `rotation_interval` 필드 값 사용). (완료)
  - [x] Next.js 웹 에디터 플레이테스트 모드(`tools/stage_editor/src/lib/game-rules.ts`)에 자동 회전 시뮬레이션 구현.
  - [x] 자동 솔버(Auto-solver) TS 탐색 알고리즘(`tools/stage_editor/src/lib/solver.ts`)에 회전 로직 지원.
- [ ] **상호작용형 동적 보드 요소**:
  - [ ] 텔레포트/포탈 셀 구현: 입구 좌표가 떨어지는 셀을 출구 좌표로 재라우팅합니다. C# `GravitySystem` 및 Next.js `game-rules.ts` 수정.
  - [ ] 컨베이어 벨트 구현: 턴 종료 시 중력 적용 전, 벨트 위에 놓인 셀을 경로 방향으로 1칸 이동시킵니다.
- [ ] **자동 보드 솔버**: 스테이지의 수학적 클리어 가능 여부를 검증하기 위해 에디터 내보내기 체크 시 통합된 다단계 AI 탐색 알고리즘(최소 이동 솔버).

## 5. 제외 범위 (Phase 2+)
- [ ] **색상 숨김 기믹**: 특정 구역이나 간격에 있는 셀의 색상을 숨겨(회색 또는 물음표로 표시) 플레이어가 색상 매칭을 추론하게 합니다. (사용자 요청에 따라 제외)
