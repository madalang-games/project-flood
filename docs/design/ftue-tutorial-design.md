# FTUE 및 튜토리얼 시스템 기획서 — project-flood

날짜: 2026-06-08
상태: 승인됨 (accepted)
관련 문서: [game-design.md](docs/design/game-design.md), [ingame-core-design.md](docs/design/ingame-core-design.md), [ui-ux-common-components.md](docs/design/ui-ux-common-components.md), [ui-ux-canvas-architecture.md](docs/design/ui-ux-canvas-architecture.md), [economy-system-design.md](docs/design/economy-system-design.md)

---

## 1. 목표 및 원칙

**목표**
- D1 이탈 방지: 첫 세션에서 규칙 이해 및 첫 클리어 보장
- 점진적 노출 (Progressive Disclosure): 코어 루프 → 기믹 → 아이템 순으로 단계별 개념 학습
- 마찰 최소화: 강제 가이드는 스테이지 1로 제한하며, 이후 단계는 상황에 맞게 노출 (Contextual)

**디자인 규칙**
1. 텍스트는 최대 한 줄로 제한 — 설명이 아닌 시각적 단서를 통해 학습 유도
2. 한 단계당 하나의 개념만 학습 — 프로텍터와 코어 셀을 동시에 설명하지 않음
3. 학습 전 보상 제공 — 스테이지 1은 플레이어가 이유를 이해하기 전에 승리를 보장함
4. 반복 없음 — 각 튜토리얼 그룹은 마지막 단계 완료 후 서버에 기록되며 다시 재생되지 않음
5. 클라이언트 텍스트에 이모지 사용 금지 — 아이콘은 텍스트 내 삽입이 아닌 별도의 UI 애셋으로 처리

---

## 2. 튜토리얼 단계

```
단계 A (강제형)       스테이지 1~3    코어 루프 습득
단계 B (상황형)       스테이지 4+     처음 만나는 기믹에 대한 설명
단계 C (실패 기반)     모든 스테이지    3회 연속 실패 시 아이템 힌트 제공
```

---

## 3. 아키텍처

### 3.1 캔버스 레이어 (Canvas Layer)

`TutorialOverlay`는 `ui-ux-canvas-architecture.md`에 따라 `Canvas_Overlay (Sort: 20)`에 위치합니다.

```
[UIManager — DontDestroyOnLoad]
  └── Canvas_Overlay (Sort: 20)
        └── TutorialOverlay          ← UIManager.ShowOverlay<TutorialOverlay>를 통해 생성/파괴
              ├── DimLayer           전체 화면 딤(Dim), 레이캐스트 차단 (alpha 0.7)
              ├── SpotlightCutout    DimLayer에 구멍을 뚫어 대상을 노출
              ├── FingerOverlay      대상 위에 애니메이션되는 손가락 아이콘
              └── TooltipBubble      대상에 고정된 말풍선
```

TutorialOverlay가 활성화된 동안 토스트(Toast, Sort: 30)는 나타나지 않아 Z-order 충돌을 방지합니다.

### 3.2 주요 컴포넌트

| 컴포넌트 | 타입 | 역할 |
|-----------|------|------|
| `TutorialManager` | MonoBehaviour (DDOL) | 단계 시퀀서 소유, 씬 이벤트 발생 시 트리거 평가 |
| `TutorialStepSequencer` | Pure C# | 단계 진행, tutorial_step 데이터 로드, 표시 명령 실행 |
| `TutorialOverlay` | MonoBehaviour (Overlay prefab) | DimLayer + SpotlightCutout + FingerOverlay + TooltipBubble 렌더링 |
| `SpotlightTarget` | Pure C# (data) | 타겟팅 모드(UI / World) 및 대상 참조 정보 보유 |

### 3.3 InGameController 연동

기존 클래스 수정을 최소화하며 두 곳에서 호출됩니다:

```csharp
// InGameController.HandleTap()
if (TutorialManager.Instance.IsBlocking) return;   // 단계 A 강제 탭 가드

// InGameController.OnBoardReady()
TutorialManager.Instance.OnBoardReady(stageId, board);   // 단계 A/B 평가 트리거
```

### 3.4 로비(Lobby) 연동

```csharp
// LobbyController.OnSceneEnter()
TutorialManager.Instance.CheckLobbyTriggers();
```

---

## 4. SpotlightOverlay — 타겟팅 시스템

두 가지 타겟팅 모드를 지원하며, `target_space` 필드에 의해 결정됩니다.

### 4.1 UI 모드 (`target_space = UI`)

`target_ui_id`가 `RectTransform`(HUD, 아이템 트레이, 결과 화면 등)을 가진 UI 요소를 참조할 때 사용됩니다.

```csharp
// RectTransform을 Canvas_Overlay 로컬 좌표로 변환
RectTransformUtility.ScreenPointToLocalPointInRectangle(
    overlayCanvas.GetComponent<RectTransform>(),
    RectTransformUtility.WorldToScreenPoint(null, targetRt.position),
    overlayCanvas.worldCamera,
    out Vector2 localPoint
);
spotlightCutout.anchoredPosition = localPoint;
spotlightCutout.sizeDelta = targetRt.rect.size * targetRt.lossyScale / overlayCanvas.scaleFactor;
```

`OnRectTransformDimensionsChange`(화면 방향 전환, 세이프 에어리어 변경 등) 발생 시 재계산합니다.

### 4.2 월드 모드 (`target_space = World`)

`target_ui_id`가 보드 셀이나 다른 씬 공간의 GameObject(예: `board_cell_[r][c]`)를 참조할 때 사용됩니다.

```csharp
// 월드 좌표를 Canvas_Overlay 로컬 좌표로 변환
Vector3 screenPos = Camera.main.WorldToScreenPoint(targetWorldPos);
RectTransformUtility.ScreenPointToLocalPointInRectangle(
    overlayCanvas.GetComponent<RectTransform>(),
    screenPos,
    overlayCanvas.worldCamera,
    out Vector2 localPoint
);
spotlightCutout.anchoredPosition = localPoint;
// 크기는 런타임에 화면에 투영된 월드 공간 경계(bounds)로부터 도출
```

`CellView`는 `GetWorldCenter()` 및 `GetScreenBounds()`를 노출하며, `SpotlightCutout`이 이를 읽습니다.
보드 애니메이션 중에는 매 프레임 재계산합니다.

### 4.3 반응형 재계산 (Responsive Recalculation)

| 이벤트 | 동작 |
|-------|--------|
| `OnRectTransformDimensionsChange` | 스포트라이트 위치 및 크기 재계산 |
| 보드 애니메이션 프레임 | 재계산 (월드 모드 전용, 로딩 시퀀스 중) |
| `Screen.safeArea` 변경 | `TutorialManager`가 `OnBoardReady` 위치 지정을 다시 트리거 |

### 4.4 클라이언트 서비스 및 뷰 로직
*   [TutorialApiService.cs](client/project-flood/Assets/Scripts/Services/TutorialApiService.cs) 추가: 로컬 캐시 및 PlayerPrefs 폴백을 포함한 튜토리얼 진행도 조회/저장 네트워크 처리.
*   [TutorialStepSequencer.cs](client/project-flood/Assets/Scripts/Services/Tutorial/TutorialStepSequencer.cs) 추가: 단계 진행 및 완료 이벤트 처리.
*   [TutorialManager.cs](client/project-flood/Assets/Scripts/Services/Tutorial/TutorialManager.cs) 추가: 스테이지 1&2 강제 온보딩, 기믹 등장, 아이템 실패 힌트 등의 트리거를 평가하는 코어 싱글톤.
*   [TutorialOverlay.cs](client/project-flood/Assets/Scripts/Core/UI/TutorialOverlay.cs) 추가: 오버레이 프리팹 생성, UI 및 월드 좌표계의 반응형 좌표 계산, 가이드 캐릭터(Floodie) 렌더링.
*   [CellView.cs](client/project-flood/Assets/Scripts/InGame/View/CellView.cs) 및 [BoardView.cs](client/project-flood/Assets/Scripts/InGame/View/BoardView.cs) 수정: 월드 셀 위치 지정을 위한 `GetWorldCenter()`, `GetScreenBounds()`, `GetCellView()` 추가.
*   [InGameController.cs](client/project-flood/Assets/Scripts/InGame/Controller/InGameController.cs) 수정: 블로킹 단계에서 잘못된 클릭을 차단하고 스포트라이트된 셀 그룹으로만 입력을 라우팅. `CS0136` 변수 명명 충돌 수정.
*   [InGameSceneEntry.cs](client/project-flood/Assets/Scripts/InGame/Controller/InGameSceneEntry.cs) 수정: PlayerPrefs에 스테이지 실패 횟수를 추적하고 실패 힌트 트리거.
*   [LobbyView.cs](client/project-flood/Assets/Scripts/OutGame/Lobby/LobbyView.cs) 수정: 시작 시 로비 트리거 평가.
*   [HomeTabView.cs](client/project-flood/Assets/Scripts/OutGame/Lobby/HomeTabView.cs) 수정: 스테이지 노드 뷰 수정 대신 별도의 보물상자 프리팹을 생성하고 상태를 바인딩하도록 리팩토링.
*   [ChapterChestView.cs](client/project-flood/Assets/Scripts/OutGame/Lobby/ChapterChestView.cs) 추가: 잠금(LOCKED), 보상 가능(CLAIM!), 획득 완료(CLEARED) 시각적 상태 및 상호작용 관리.
*   [UIPulseGlowEffect.cs](client/project-flood/Assets/Scripts/Core/UI/UIPulseGlowEffect.cs) 추가: 활성화된 상자에 펄스 및 회전 애니메이션 추가.
*   [UIPulseGlow.shader](client/project-flood/Assets/Shaders/UIPulseGlow.shader) 추가: UI 마스크 스텐실 버퍼 체크를 지원하는 고품질 절차적 글로우 외곽선 쉐이더.
*   [PrefabSetupUtility.cs](client/project-flood/Assets/Scripts/Editor/PrefabSetupUtility.cs) 추가: `Tools/Project Flood/Generate UI Prefabs` 메뉴를 통해 `TutorialOverlay.prefab` 및 `ChapterChest.prefab` 계층 구조를 자동으로 구축하고 Resources 폴더에 저장하는 에디터 툴.

---

## 5. 단계 A — 코어 루프 온보딩 (스테이지 1~3)

### 5.1 스테이지 1 — 강제 가이드

**보드 요구 사양 (스테이지 1 전용):**

| 필드 | 값 | 근거 |
|-------|-------|-----------|
| board_width | 6 | 작게 구성 — 인지 부하 감소 |
| board_height | 6 | 작게 구성 |
| turn_limit | 20 | 넉넉하게 부여 — 실패 불가능 |
| color_count | 3 | 최소 색상 수 |
| star1_ratio | 0.70 | 기본 0.80보다 낮게 설정 — 비효율적으로 플레이해도 클리어 보장 |
| difficulty | tutorial | |
| gimmicks | none | 코어 / 프로텍터 / 장애물 없음 |

**3회 터치 클리어 보장 (보드 레이아웃 계약):**

스테이지 1은 가장 큰 3개 그룹을 탭하면 보드의 70% 이상이 제거되도록 설계되어야 합니다.
에디터 조작자는 수출(Export) 전에 `verifiedSolution`으로 이를 검증해야 합니다.

**단계 시퀀스:**

| step_index | content_type | target_ui_id | target_space | text_key | auto_advance_sec | is_blocking |
|-----------|-------------|-------------|-------------|---------|-----------------|------------|
| 0 | `finger_overlay` | `board_cell_[r][c]` (가장 큰 그룹의 대표 셀) | World | `tut.tap_group` | 0 | true |
| 1 | `tooltip` | `board_area` | World | `tut.gravity_explain` | 2.0 | false |
| 2 | `highlight_only` | `hud_turn_count` | UI | `tut.turn_explain` | 2.0 | false |
| 3 | `highlight_only` | `hud_ratio_bar` | UI | `tut.ratio_explain` | 2.0 | false |
| 4 | `tooltip` | `result_star_area` | UI | `tut.star_explain` | 0 | false |

**강제 탭 동작 (단계 0):**
- `DimLayer` 활성화 (alpha 0.7, 전체 레이캐스트 차단)
- `SpotlightCutout`이 가장 큰 색상 클러스터 전체를 노출
- `FingerOverlay`가 그룹의 중심(대표 셀)에 위치
- 다른 모든 보드 셀 및 HUD는 레이캐스트 차단
- 스포트라이트 영역 외의 탭은 무시됨

### 5.2 스테이지 2 — 반강제 가이드

| step_index | content_type | target_ui_id | target_space | text_key | auto_advance_sec | is_blocking |
|-----------|-------------|-------------|-------------|---------|-----------------|------------|
| 0 | `highlight_only` | `board_area` | World | `tut.free_tap` | 0 | false |

플레이어의 첫 탭 시 단계가 진행됩니다. 강제 방향은 없습니다.

### 5.3 스테이지 3 — 자유 플레이

튜토리얼 단계가 없습니다. 스테이지 3 진입 후 단계 A 완료 플래그가 설정됩니다.

---

## 6. 단계 B — 상황별 기믹 소개

각 기믹 유형을 처음 만날 때 트리거됩니다. (`trigger_type = gimmick_appear`)
`OnBoardReady()`에서 평가하며, `user_tutorial_progress`에 기록되지 않은 기믹이 보드 상태에 있는지 확인합니다.

### 6.1 프로텍터 셀 (Protector Cell)

| step_index | content_type | target_ui_id | target_space | text_key | auto_advance_sec | is_blocking |
|-----------|-------------|-------------|-------------|---------|-----------------|------------|
| 0 | `highlight_only` | `board_protector_cell` (첫 프로텍터 셀) | World | `tut.protector_what` | 2.0 | false |
| 1 | `finger_overlay` | `board_protector_cell` | World | `tut.protector_how` | 2.0 | false |

### 6.2 코어 셀 (Core Cell)

| step_index | content_type | target_ui_id | target_space | text_key | auto_advance_sec | is_blocking |
|-----------|-------------|-------------|-------------|---------|-----------------|------------|
| 0 | `tooltip` | `board_core_cell` (첫 코어 셀) | World | `tut.core_warning` | 3.0 | false |

경고 아이콘은 텍스트 내 삽입이 아닌 TooltipBubble 옆에 위치하는 별도의 UI 애셋으로 렌더링됩니다.

### 6.3 장애물 셀 (Obstacle Cell)

| step_index | content_type | target_ui_id | target_space | text_key | auto_advance_sec | is_blocking |
|-----------|-------------|-------------|-------------|---------|-----------------|------------|
| 0 | `tooltip` | `board_obstacle_cell` (첫 장애물 셀) | World | `tut.obstacle_what` | 3.0 | false |

---

## 7. 단계 C — 실패 트리거 아이템 힌트

### 7.1 트리거 조건

```
fail_count[stage_id] >= 3
AND ItemInventory.CanUse(any type) == true
AND PlayerPrefs.GetInt("item_hint_shown_" + stage_id) == 0
```

### 7.2 단계 시퀀스

| step_index | content_type | target_ui_id | target_space | text_key | auto_advance_sec | is_blocking |
|-----------|-------------|-------------|-------------|---------|-----------------|------------|
| 0 | `finger_overlay` | `item_tray` | UI | `tut.item_hint_prompt` | 3.0 | false |
| 1 | `tooltip` | `item_slot_bomb` | UI | `tut.item_bomb_effect` | 2.0 | false |

---

## 8. 데이터 스키마

### tutorial_step (CSV → info_generator)

| 컬럼 | 타입 | 설명 |
|--------|------|-------|
| id | INT PK | |
| trigger_type | ENUM | `first_launch` / `gimmick_appear` / `fail_repeat` 등 |
| trigger_value | VARCHAR | stage_id, 기믹 유형 명칭 등 |
| step_index | INT | 동일 그룹 내 순서 (0부터 시작) |
| content_type | ENUM | `finger_overlay` / `tooltip` / `highlight_only` |
| target_ui_id | VARCHAR | 런타임에 RectTransform 또는 월드 오브젝트로 해석될 ID |
| target_space | ENUM | `UI` (RectTransform) / `World` (월드 좌표) |
| text_key | VARCHAR | 로컬라이제이션 키 |
| auto_advance_sec | FLOAT | 자동 진행 시간 (0이면 사용자 조작 대기) |
| is_blocking | BOOL | true이면 대상 외의 모든 입력 차단 |

---

## 9. UI 컴포넌트

### TooltipBubble (말풍선)
- 말풍선 패널, 아이콘 슬롯(선택), 텍스트(TMP)로 구성.
- 꼬리(Tail) 스프라이트는 대상 위치에 따라 상/하/좌/우로 자동 회전.

### FingerOverlay (손가락)
- 대상 중심으로부터 오프셋 위치에 배치.
- 누르는 애니메이션 반복.

### SpotlightCutout (스포트라이트)
- 전체 화면 DimLayer와 커스텀 쉐이더 또는 RectMask2D를 이용한 구멍 뚫기.
- 단계 변경 시 타겟 위치로 DOTween을 이용한 부드러운 이동 및 크기 변경.

---

## 10. 로컬라이제이션 키 (Localization Keys)

| 키 | 한국어 텍스트 |
|-----|---------|
| `tut.tap_group` | 같은 색 셀들을 탭하세요! |
| `tut.gravity_explain` | 셀이 아래로 떨어져요 |
| `tut.turn_explain` | 턴을 아껴서 더 많이 지워보세요 |
| `tut.ratio_explain` | 이 바를 채울수록 별을 더 받아요 |
| `tut.star_explain` | 별 3개면 완벽 클리어! |
| `tut.free_tap` | 이제 직접 탭해보세요! |
| `tut.protector_what` | 이 셀에는 보호막이 있어요 |
| `tut.protector_how` | 같은 색 그룹을 탭하면 벗겨져요 |
| `tut.core_warning` | 이 셀을 제거해야 스테이지 클리어가 돼요 |
| `tut.obstacle_what` | 이 셀은 탭으로 제거할 수 없어요 |
| `tut.item_hint_prompt` | 아이템을 써보세요! |
| `tut.item_bomb_effect` | 폭탄: 주변 셀을 한 번에 제거해요 |
| `tut.rotation_explain` | 보드가 뒤집혔어요! 중력이 새 방향으로 적용돼요 |

---

## 11. MVP 범위

### 포함 사항
- 단계 A: 스테이지 1 강제 가이드 (5단계)
- 단계 A: 스테이지 2 반강제 가이드 (1단계)
- 단계 B: 프로텍터/코어/장애물 첫 만남 힌트
- 단계 C: 3회 실패 시 아이템 힌트
- UI/월드 좌표 대응 스포트라이트 시스템
- 응답형 좌표 재계산
- 서버 및 로컬 캐시 기반 진행도 관리

### 제외 사항 (2단계)
- 보드 회전 힌트
- 아이템 없을 시 광고 유도 CTA
- 게스트 → 서버 진행도 마이그레이션 로직
