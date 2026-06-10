# 현재 프로젝트 상태 및 로드맵

현재 단계: **MVP 클라이언트-서버 통합**  
중점 사항: 클라이언트 씬과 실시간 서버 API 연결, 스테미나 게이트 안정화, 실제 인증 핸드셰이크 구현.

---

## 1. 기능 영역별 체크리스트
| 파일 | 영역 | MVP 상태 |
|------|------|-----------|
| [01_gameplay_ingame.md](01_gameplay_ingame.md) | 핵심 퍼즐 규칙 및 게임 루프 | ✅ MVP 완료 |
| [02_items_boosters.md](02_items_boosters.md) | 5종 아이템 전체 (Bomb/HRocket/ColorSweep/RowShift/CellSwap) | ✅ 로직 완료 — VFX 폴리싱 필요 |
| [03_stamina_economy.md](03_stamina_economy.md) | 스테미나 게이트 및 골드 경제 | ✅ MVP 완료 |
| [04_lobby_progression.md](04_lobby_progression.md) | 로비 로드맵 및 리더보드 | ✅ MVP 완료 |
| [05_stage_editor.md](05_stage_editor.md) | 웹 스테이지 에디터 및 솔버(Solver) | ✅ MVP 완료 |
| [06_auth_infrastructure.md](06_auth_infrastructure.md) | 플랫폼 인증 및 DB/Redis 스택 | ✅ MVP 완료 |
| [07_ads_monetization.md](07_ads_monetization.md) | AdMob 보상형/전면 광고 | ✅ MVP 완료 |
| [08_polish_ux_sfx.md](08_polish_ux_sfx.md) | 비주얼 폴리싱, VFX, 오디오 | [/] 기본 완료 — 쥬스/SFX 미시작 |

---

## 2. MVP 블로커 (소프트 런칭 전 해결 필수)

배포 가능한 MVP를 위해 남은 과제들입니다:

### ~~A. 보상형 광고 검증 UX [07]~~ ✅ 완료
- 클라이언트 nonce 전송; 서버 SSV 콜백 수신 — 흐름 연결됨
- 광고 종료 후 모달 스피너 표시; 닫기 전 확인을 위해 서버 상태 폴링

### ~~B. 스테미나 게이트 E2E~~ ✅ 완료
### ~~C. 골드 서버 동기화~~ ✅ 완료
### ~~D. Unity 클라이언트 인증~~ ✅ 완료

---

## 3. 진행 중 (활성 스프린트)
- [/] **VFX 및 오디오 쥬스(Juice)**: 게임 효과음 추가, SoundManager를 통한 배경음악 처리, 퍼즐 비주얼 폴리싱.

---

## 4. 최근 완료된 항목
- [x] **스테미나 게이트 E2E**: `LobbyView` 로딩 시 `StaminaApiService.FetchStamina()` 호출; `HeaderView` 실시간 데이터 렌더링; `StageInfoPopupView` 입장 제한 기능.
- [x] **골드 서버 동기화**: `CurrencyApiService` (GET + POST /spend); 로비 로딩 시 골드 조회; 스테이지 클리어 시 서버 응답으로 정산; 이어하기 시 서버 측 차감.
- [x] **인증 통합**: `AuthService`가 이미 `/api/auth/guest` + JWT Bearer와 연결됨 — 완료 확인.
- [x] **5종 아이템 전체 구현**: ColorSweep, RowShift (스와이프 단계), CellSwap (두 번 터치 단계), Bomb, HRocket — 전체 로직 + InGameController 입력 라우팅.
- [x] **VRocket 제거**: 개편된 아이템 디자인에 따라 ColorSweep/RowShift/CellSwap으로 교체.
- [x] **StaminaPopupView**: 하트 개수, 카운트다운 타이머, 광고 보기 버튼이 포함된 스테미나 팝업.
- [x] **StageInfoPopupView 스테미나 체크**: 플레이 허용 전 `StaminaApiService`를 통한 입장 게이트 체크.
- [x] **Lobby AGENTS.md 업데이트**: HeaderView 스테미나 필드, StaminaPopupView 심볼 문서화.
- [x] **웹 스테이지 에디터 플레이테스트 모드 및 TS 솔버 통합**.
- [x] **광고 검증 알림을 위한 서버 측 AdSsvCallbackController**.
- [x] **Unity 내 C# 규칙 엔진 (매칭, 중력, 180도 보드 회전, 프로텍터 스트립, 코어 셀)**.
- [x] **트랜잭션 API를 위한 EventLog 감사 기록**.
- [x] **Docker-compose MySQL/Redis 개발 환경**.
