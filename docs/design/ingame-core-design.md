# 인게임 코어 디자인 (InGame Core Design)

## 아키텍처

순수 C# 규칙 엔진 + MonoBehaviour 뷰 레이어. 근거는 ADR-006을 참조하세요.

```
입력 (New Input System)
  → InGameController (MonoBehaviour)
      ├── StageLoader        CSV 읽기 → BoardState 생성
      ├── GroupSelector      BFS를 이용한 동일 색상 그룹 선택
      ├── RemovalSystem      그룹 제거 및 프로텍터 처리
      ├── GravitySystem      하향 열 패킹(Packing)
      ├── TurnManager        턴 추적
      └── ClearEvaluator     비율 + 코어 + 별 결과 평가
            ↓
      BoardView / CellView (MonoBehaviour, 보드 상태로부터 읽기 전용으로 동작)
```

규칙 엔진 클래스들은 **UnityEngine 의존성이 전혀 없습니다.**

---

## 모듈 상세 분석

### Board/ (순수 C# 데이터)

| 클래스 | 역할 |
|-------|------|
| `CellType` | 열거형 — Basic=0, Obstacle=1 (`GameEnums.cs`와 미러링) |
| `CellData` | 구조체 — color_id, CellType, protector_strength (0–2), is_core 포함 |
| `BoardState` | 2D `CellData?[,]` 그리드 + `initial_valid_cells` 개수 관리 |

`null` 셀 = 빈 슬롯 (제거 후 중력 적용 전, 또는 경계 밖).

### Rules/ (순수 C# 알고리즘)

| 클래스 | 역할 |
|-------|------|
| `GroupSelector` | 탭 위치로부터 BFS 수행; `List<(int row, int col)>` 반환. 크기 1 이상이면 항상 유효. |
| `RemovalSystem` | 그룹을 순회하며 각 셀에 대해 ProtectorSystem 호출; protector_strength가 0인 셀 제거 |
| `ProtectorSystem` | 직접 타격 로직: protector_strength 감소; 0이 되면 밑바닥 셀 노출 |
| `GravitySystem` | 열 단위: null이 아닌 셀들을 아래로 압축; 위쪽은 null로 채움 |
| `ClearEvaluator` | 클리어 비율 계산; 코어 제거 확인; `StarResult` 반환 |

### Controller/ (MonoBehaviour 레이어)

| 클래스 | 역할 |
|-------|------|
| `StageLoader` | CTM 헥사 문자열 파싱 → `BoardState` 생성; `color_ids` 파싱 |
| `TurnManager` | `remaining_turns` 추적; `Consume()`은 남은 턴 여부 반환 |
| `InGameController` | MonoBehaviour 오케스트레이터; 규칙 엔진 인스턴스 소유; 탭 → 결과 흐름 구동 |

### View/ (MonoBehaviour, 렌더링 전용)

| 클래스 | 역할 |
|-------|------|
| `BoardView` | `CellView` 그리드 생성 및 배치; 보드 변경 시 업데이트 |
| `CellView` | 단일 셀 렌더링: 색상, 유형 스프라이트, 프로텍터 오버레이, 코어 표시기 |

---

## 탭 흐름 (Tap Flow)

```
1. 플레이어가 화면 위치 탭
2. InGameController → 히트 테스트 → (행, 열) 도출
3. GroupSelector.FindGroup(board, row, col)
   → 4방향 인접 동일 색상 BFS 수행
   → List<(row,col)> 반환 (크기 ≥ 1)
4. RemovalSystem.Remove(board, group)
   → 그룹 내 각 셀에 대해:
       ProtectorSystem.DirectHit(cell)
         protector_strength > 0 → 강도 감소 (셀 유지)
         protector_strength = 0 → board[r,c] = null (셀 제거)
5. GravitySystem.Apply(board)
   → 각 열별로 null이 아닌 셀들을 아래로 채움
6. TurnManager.Consume()
   → 남은 턴 감소
7. ClearEvaluator.Evaluate(board, initialValidCells, hasCoreFlag)
   → 클리어 비율 = (초기 유효 셀 - 남은 유효 셀) / 초기 유효 셀
   → 코어 클리어 여부 = 남은 셀 중 is_core가 없는지 확인
   → StarResult 반환: 실패 / 별1 / 별2 / 별3
8. InGameController에서 StarResult 처리
   → 별3개 획득 또는 턴 0 → 스테이지 종료
   → 그 외 → 다음 탭 대기
```

---

## 클리어 조건

```
initial_valid_cells = 전체 보드 셀 - 장애물(Obstacle) 셀 (스테이지 로드 시 계산)
remaining_valid     = 평가 시점에 보드에 남은 null이 아니고 장애물이 아닌 셀들

클리어 비율 = (initial_valid_cells - remaining_valid) / initial_valid_cells

승리(WIN)  = 클리어 비율 ≥ star1_ratio AND 코어 클리어됨
실패(FAIL) = 클리어 비율 < star1_ratio OR 코어 미제거

별(Stars):
  3 = 모든 유효 셀 제거 (remaining_valid = 0) ← 조기 종료
  2 = 클리어 비율 ≥ star2_ratio
  1 = 클리어 비율 ≥ star1_ratio
```

---

## 스테이지 로딩

`StageLoader` 입력: `StageRow` (`stage.csv`에서 생성된 C# 모델).

```
cells 문자열  → 3자리의 CTM 헥사 청크로 분할
청크 인덱스 i → 행 = i / 너비, 열 = i % 너비
C 헥사 문자   → 컬러 ID (0–15)
T 헥사 문자   → 셀 유형 (0=기본, 1=장애물)
M 헥사 문자   → 프로텍터 강도 = M & 0x3, 코어 여부 = (M & 0x4) != 0
```

---

## 네임스페이스 컨벤션

```
Game.InGame.Board      → CellData, BoardState
Game.InGame.Rules      → GroupSelector, RemovalSystem, ProtectorSystem, GravitySystem, ClearEvaluator
Game.InGame.Controller → InGameController, TurnManager, StageLoader
Game.InGame.View       → BoardView, CellView
```
