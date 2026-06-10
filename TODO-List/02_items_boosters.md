# 아이템 및 부스터 시스템 체크리스트

일반적인 터치-그룹 흐름 외에 보드 상태를 수정하는 플레이어 활성화 부스터(폭탄, 가로 로켓, 색상 제거, 행 이동, 셀 교체)와 그 인벤토리 관리, UI/UX 및 서버 측 추적을 위한 체크리스트입니다.

## 1. 아이템 정의 및 보드 상호작용 (MVP)
- [x] **폭탄 (3x3 영역)**: 타겟 셀을 중심으로 3x3 그리드 내의 모든 셀(기본, 코어, 장애물)을 제거합니다. 영향을 받은 셀의 프로텍터 레이어를 하나 벗겨냅니다.
  - 참조: [BombEffect.cs](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/client/project-flood/Assets/Scripts/InGame/Items/BombEffect.cs)
- [x] **가로 로켓 (행 청소)**: 왼쪽에서 오른쪽으로 훑습니다. Void 위치는 건너뜁니다(계속 진행). 기본/코어/프로텍터(한 레이어 제거)를 파괴합니다. 첫 번째 장애물 셀에서 즉시 멈춥니다(파괴 후 정지).
  - 참조: [HRocketEffect.cs](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/client/project-flood/Assets/Scripts/InGame/Items/HRocketEffect.cs)
- [x] **색상 제거 (ColorSweep)**: 터치한 셀의 색상 ID와 일치하는 보드 위의 모든 셀을 제거합니다. 장애물/Void는 영향을 받지 않습니다. 프로텍터 셀은 RemovalSystem을 통해 한 레이어를 벗겨냅니다. ADR-007 참조.
  - 참조: [ColorSweepEffect.cs](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/client/project-flood/Assets/Scripts/InGame/Items/ColorSweepEffect.cs)
- [x] **행 이동 (RowShift, 수평 압착)**: 행 이동 사용 단계 중 보드에서의 스와이프 제스처(왼쪽 또는 오른쪽). 스와이프 방향으로 각 행의 모든 셀을 밀어 넣어 빈 슬롯을 없앱니다. Void 위치는 각 행 세그먼트의 딱딱한 경계 역할을 합니다. 최소 50px 스와이프 거리 임계값이 적용됩니다. 이후 GravitySystem이 실행됩니다. ADR-007 참조.
  - 참조: [RowShiftEffect.cs](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/client/project-flood/Assets/Scripts/InGame/Items/RowShiftEffect.cs) 및 [InGameController.cs](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/client/project-flood/Assets/Scripts/InGame/Controller/InGameController.cs)
- [x] **셀 교체 (두 셀 위치 교체)**: 두 번 터치 흐름 — 첫 번째 유효한 터치 시 원본 셀 강조; 두 번째 유효한 터치 시 교체 실행. 원본 셀을 다시 터치하면 선택 해제됩니다. 교체 후 중력은 적용되지 않습니다(셀이 제자리에서 교환됨). GravitySystem은 실행되지 않습니다. ADR-007 참조.
  - 참조: [CellSwapEffect.cs](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/client/project-flood/Assets/Scripts/InGame/Items/CellSwapEffect.cs) 및 [ItemManager.cs](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/client/project-flood/Assets/Scripts/InGame/Items/ItemManager.cs)
- [x] **턴 미소모**: 아이템 사용은 턴을 소모하지 않습니다. 남은 턴이 0이 되면 잠기며 사용할 수 없습니다.
  - 참조: [InGameController.cs:L101](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/client/project-flood/Assets/Scripts/InGame/Controller/InGameController.cs#L101)
- [x] **개발 모드 (무한 인벤토리)**: `InGameController`의 인스펙터 토글 `IsDevMode`는 소모를 건너뛰고 "∞" 배지를 표시합니다.
  - 참조: [ItemInventory.cs](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/client/project-flood/Assets/Scripts/InGame/Items/ItemInventory.cs)

## 2. 인게임 UI/UX 흐름 (MVP)
- [x] **아이템 트레이 뷰 레이아웃**: 하단에 사용 가능한 아이템 아이콘과 개수를 표시하는 레이아웃입니다. 개수가 0이면 회색으로 표시됩니다. 5개의 슬롯(Bomb, HRocket, ColorSweep, RowShift, CellSwap)을 보여줍니다.
  - 참조: [ItemTrayView.cs](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/client/project-flood/Assets/Scripts/InGame/View/ItemTrayView.cs)
- [x] **타겟팅 사용 단계 (터치-투-타겟)**: 슬롯 터치 -> 슬롯 발광 -> 보드 셀 펄스 효과. 보드 셀을 터치하면 즉시 아이템이 실행됩니다. 슬롯을 다시 터치하거나 바깥쪽을 터치하면 취소됩니다. 폭탄, 가로 로켓, 색상 제거에 적용됩니다.
  - 참조: [ItemManager.cs](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/client/project-flood/Assets/Scripts/InGame/Items/ItemManager.cs)
- [x] **행 이동 스와이프 단계**: 행 이동이 선택되면 보드는 터치 대신 수평 스와이프 입력을 캡처합니다. 최소 50px 델타 임계값이 적용됩니다. 짧거나 수직인 스와이프는 무시되며, 보드 경계 밖에서 떼면 아이템이 취소됩니다. 유효한 non-Void, non-Obstacle 셀 기점에서만 트리거됩니다.
  - 참조: [InGameController.cs](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/client/project-flood/Assets/Scripts/InGame/Controller/InGameController.cs)
- [x] **셀 교체 두 번 터치 단계**: FirstCellSelected 상태 머신 — 첫 번째 유효한 터치 시 원본 셀 강조(SetCellSelectedHighlight); 두 번째 유효한 터치 시 교체 실행; 동일한 셀을 다시 터치하면 선택 해제. 슬롯 재터치 또는 보드 외부 터치 시 취소됩니다.
  - 참조: [InGameController.cs](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/client/project-flood/Assets/Scripts/InGame/Controller/InGameController.cs) 및 [ItemManager.cs](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/client/project-flood/Assets/Scripts/InGame/Items/ItemManager.cs)
- [/] **VFX 애니메이션 피드백**: BoardView에 PlayRowShift 및 PlayCellSwap 코루틴이 구현되어 있습니다. 기본적인 슬라이드/교체 애니메이션이 구현되었습니다. 폭탄, 로켓, 색상 제거, 행 이동, 셀 교체에 대해 하이퍼 캐주얼 등급의 활기찬 쥬스/폴리싱(파티클 버스트, 컬러 웨이브, 사운드)이 추가로 필요합니다.
  - 상태: [BoardView.cs](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/client/project-flood/Assets/Scripts/InGame/View/BoardView.cs)에 PlayRowShift 및 PlayCellSwap이 존재합니다. 추가적인 비주얼 플래시와 임팩트 있는 파티클 효과가 필요합니다.

## 3. 부스터 및 인벤토리 확장 (활성 범위)
- [ ] **서버 기반 아이템 인벤토리**: 플레이어별 아이템 인벤토리 개수를 서버 DB에 유지하고, 클라이언트 로그인 핸드셰이크를 통해 동기화합니다.
  - [ ] DB 트랜잭션 소모 `/api/inventory/spend` 및 인벤토리 동기화 API `/api/inventory` 구현.
- [ ] **인게임 아이템 상점 구매**: 개수가 0일 때 아이템 슬롯에 UI 버튼이나 비용 배지를 추가하여 골드로 즉시 부스터를 구매할 수 있게 합니다(폭탄 1개 = 100 골드).
  - [ ] 골드를 소모하고 아이템을 추가하는 `/api/inventory/buy` 엔드포인트 생성.
  - [ ] 아이템 개수가 0일 때 골드 구매 배지를 렌더링하도록 `ItemSlotView` 및 `ItemTrayView` 업데이트.
- [ ] **게임 시작 전 부스터 선택**: 씬에 진입하기 전 로비의 `StageInfoPopupView`에서 부스터(예: "+3 시작 턴", 시작 폭탄/로켓)를 선택할 수 있게 합니다.
  - [ ] 스테이지 로딩 시 즉시 부스터 개수를 차감합니다.
  - [ ] 스테이지 로딩 시 선택된 부스터를 무작위 좌표에 미리 생성합니다.
- [ ] **연승 부스터 (Win Streaks)**: 스테이지를 연속으로 클리어하여 보드에 배치되는 무료 시작 부스터를 획득합니다(티어 1: 로켓 1개, 티어 2: 로켓 1개 + 폭탄 1개, 티어 3: 로켓 1개 + 폭탄 1개 + 색상 제거 1개).
  - [ ] 서버 DB에서 연승 횟수를 추적하며, 스테이지 실패/포기 시 초기화됩니다.
  - [ ] 스테이지 로딩 시 유효한 보드 좌표에 연승 부스터를 자동으로 생성합니다.

## 4. 폴리싱 (활성 범위)
- [ ] **역동적인 아이템 VFX/SFX**: 활성화된 테마 챕터의 비주얼 팔레트와 일치하는 임팩트 있는 사운드 효과(쉬익, 폭발, 금속 타격음)와 파티클 불꽃을 추가합니다. (Phase N에서 이동됨)
