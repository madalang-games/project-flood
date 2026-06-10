# 스테이지 에디터 및 콘텐츠 툴링 체크리스트

스테이지 구성, 솔루션 기록 및 게임 규칙 검증에 사용되는 독립형 Next.js 스테이지 에디터(`tools/stage_editor/`)를 위한 체크리스트입니다.

## 1. 웹 에디터 캔버스 및 UI (MVP)
- [x] **그리드 보드 캔버스**: 페인트 브러시가 포함된 최대 16x16 크기의 그리드입니다. 왼쪽 클릭으로 브러시 셀 상태를 칠하고, 오른쪽 클릭으로 지웁니다.
  - 참조: [BoardEditor.tsx](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/tools/stage_editor/src/components/BoardEditor.tsx)
- [x] **셀 브러시 인스펙터**: 브러시 속성(CellType: 기본, 장애물, Void), 컬러 팔레트 피커(16개 색상), 프로텍터 레이어(0-2), 코어 지정을 설정합니다.
  - 참조: [CellInspector.tsx](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/tools/stage_editor/src/components/CellInspector.tsx)
- [x] **메타데이터 패널**: 스테이지 필드(너비, 높이, 턴 제한, 난이도, 커스텀 별 비율, 서버 보상 그룹 ID)를 편집합니다.
  - 참조: [MetadataPanel.tsx](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/tools/stage_editor/src/components/MetadataPanel.tsx)
- [x] **스테이지 생성 모드**: 프리셋 파라미터(색상 수, 장애물 밀도, 프로텍터 빈도, 코어 개수)를 기반으로 보드를 자동 생성합니다.
  - 참조: [GeneratorPanel.tsx](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/tools/stage_editor/src/components/GeneratorPanel.tsx)

## 2. 플레이테스트 및 엑스포트 검증 (MVP)
- [x] **플레이테스트 모드**: TS에서 게임 규칙을 실행하는 시뮬레이션 플레이테스트입니다. 일치하는 그룹 터치, 중력 적용, 프로텍터 제거, 180도 회전 기믹 트리거가 가능합니다.
  - 참조: [PlaytestPanel.tsx](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/tools/stage_editor/src/components/PlaytestPanel.tsx) 및 [game-rules.ts](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/tools/stage_editor/src/lib/game-rules.ts)
- [x] **솔루션 레코더**: 성공적인 플레이테스트 중 플레이어의 터치를 기록하고 `verified_solution` 경로 문자열 시퀀스로 저장합니다.
  - 참조: [PlaytestPanel.tsx](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/tools/stage_editor/src/components/PlaytestPanel.tsx)
- [x] **엑스포트 검증**: 규칙 세트 일치 여부를 확인하고, 기록된 솔루션이 시뮬레이션을 통해 현재 보드 상태를 성공적으로 클리어하는지 검증하며 경고를 표시합니다.
  - 참조: [validator.ts](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/tools/stage_editor/src/lib/validator.ts)
- [x] **자동 솔버(Solver 통합)**: 스테이지를 자동으로 해결하고 클리어 가능성을 검증하기 위한 BFS 최소 이동 탐색(최대 5,000개 상태) 및 그리디(greedy) 폴백 기능입니다.
  - 참조: [solver.ts](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/tools/stage_editor/src/lib/solver.ts)

## 3. CSV 통합 및 파이프라인 (MVP)
- [x] **Next.js CRUD API 라우트**: `shared/datas/stage/stage.csv` 내의 스테이지 행을 직접 조회, 업데이트, 삭제, 추가하기 위한 API 엔드포인트입니다.
  - 참조: [route.ts](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/tools/stage_editor/src/app/api/stages/route.ts)
- [x] **CTM 헥스(Hex) 인코딩**: CSV의 CTM 형식을 `CellData`의 2D 그리드로 디코딩하고, 저장 시 다시 인코딩합니다. 고유한 스테이지 색상을 자동으로 도출합니다.
  - 참조: [ctm.ts](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/tools/stage_editor/src/lib/ctm.ts)

## 4. 콘텐츠 생성 및 에디터 확장 (활성 범위)
- [ ] **이미지-투-보드 자동 드래프팅**: 이미지 파일을 드래그-앤-드롭하면 그리드 크기에 맞춰 픽셀화하고, 색상을 가장 가까운 LAB 색 공간 헥스 값에 매핑하며, 고립된 색상 노드를 자동으로 수정합니다.
- [ ] **고급 솔버 지표**: 스테이지 페이싱 밸런싱을 돕기 위해 클리어 난이도 등급(예: 상태 공간 밀도, 분기 계수, 필요한 최소 이동 횟수)을 기록하고 표시합니다.
- [ ] **Unity 에디터 내 핫 리로딩(Hot-Reloading)**: 웹 에디터에서 저장할 때 Unity에서 즉시 info_generator 파이프라인을 트리거하고 클라이언트 스테이지 에셋을 새로고침하는 에디터 스크립트를 생성합니다.
- [ ] **캔버스 작업 실행 취소/다시 실행**: 수동 스테이지 디자인 작업 속도를 높이기 위해 웹 캔버스에서 표준 키보드 단축키(Ctrl+Z / Ctrl+Y)를 지원합니다.
