# 플랫폼 인증, DB 및 인프라 체크리스트

서버 배포 파이프라인, 데이터베이스 매핑, Redis 상태 캐싱, 플랫폼 JWT 인증, 이벤트 감사 로그 및 개발자 스택을 위한 체크리스트입니다.

## 1. Stateless 인증 및 사용자 식별 (MVP)
- [x] **플랫폼 JWT Stateless 검증**: 캐싱된 JWKS 공개 키를 사용하여 토큰 서명을 검증합니다.
  - 참조: [JwtPublicKeyCache.cs](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/server/src/ProjectFlood.Infrastructure/Security/JwtPublicKeyCache.cs)
- [x] **사용자 ID 확인 미들웨어**: 외부 JWT PID(`sub`)를 내부 증가 UID와 매칭하고 커스텀 클레임을 등록합니다.
  - 참조: [UserIdResolutionMiddleware.cs](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/server/src/ProjectFlood.API/Middleware/UserIdResolutionMiddleware.cs)
- [x] **Unity 클라이언트 AuthService 서버 통합**: 스탯을 안전하게 동기화하기 위해 Guest 디바이스 ID 스텁(stub) 로직을 실제 JWT 인증 헤더 핸드셰이크로 교체합니다.
  - 상태: 완료. `AuthService.Initialize()`가 `/api/auth/guest`(또는 토큰 존재 시 `/api/auth/refresh`)를 호출하고, `SecureTokenStorage`를 통해 JWT를 저장하며, 모든 API 호출에 `Authorization: Bearer` 헤더를 첨부하도록 `NetworkService.SetAuthToken()`을 호출합니다. 에디터에서 서버 연결이 불가능한 경우 오프라인 게스트로 폴백합니다.

## 2. 인프라 및 캐시 (MVP)
- [x] **Docker Compose 스택**: 로컬 MySQL DB 및 Redis 캐시 인스턴스를 위한 단일 스크립트 compose 설정입니다.
  - 참조: [docker-compose.yml](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/docker-compose.yml) 및 [docker-compose.dev.bat](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/docker-compose.dev.bat)
- [x] **Redis 시도(Attempt) 캐시**: 설정 제한에 따른 TTL 타임아웃 값을 가진 휘발성 활성 스테이지 시도 데이터를 저장합니다.
  - 참조: [StageAttemptService.cs:L63](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/server/src/ProjectFlood.Application/Stage/StageAttemptService.cs#L63)
- [x] **DB 스키마 제너레이터**: 단일 JSON 스키마 정의로부터 EF 매핑과 SQL 마이그레이션 스크립트를 동적으로 자동 생성합니다.
  - 참조: [schema.json](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/server/db/schema.json) 및 [db_generator.bat](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/tools/db_generator.bat)

## 3. 데이터베이스 및 감사 로그 (MVP)
- [x] **EF Core ORM 및 DB Context**: 생성된 MySQL 엔티티를 EF DbContext에 매핑하며, snake_case 컬럼 이름과 외래 키 참조를 강제합니다.
  - 참조: [AppDbContext.cs (생성됨)](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/server/generated/scripts/Infrastructure/AppDbContext.g.cs)
- [x] **이벤트 로그 감사 추적**: 로그 팩토리가 주요 데이터베이스 수정 사항(스테미나 변동, 보상 수령, 스테이지 클리어, 광고 시청)에 대해 `event_logs`에 항목을 추가합니다.
  - 참조: [EventLogFactory.cs (생성됨)](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/server/generated/scripts/Application/EventLogFactory.g.cs)

## 4. 인증 및 API 확장 (활성 범위)
- [ ] **플랫폼 OAuth API 연결**: 게스트 계정을 서버의 영구 소셜 프로필로 인증하기 위해 로그인 제공자(Google Play Games, Apple Game Center)를 추가합니다.
- [ ] **속도 제한(Rate Limiting) 미들웨어**: 클라이언트 치트 스크립트 악용을 방지하기 위해 트랜잭션 스테이지 엔드포인트(시도 시작, 클리어, 보상 수령)에 대한 속도 제한 정책을 확장합니다.

## 5. 제외 범위 (Phase 2+)
- [ ] **Redis 클러스터 및 DB 장애 조치(Failover)**: 운영 환경 배포 시 고가용성을 보장하기 위해 클러스터 토폴로지, 레플리카 설정 및 자동 장애 조치 전략을 구성합니다. (사용자 요청에 따라 제외)
- [ ] **분석 이벤트 디스패처**: 비즈니스 인텔리전스 대시보드 구성을 위해 분석 수집기(예: BigQuery 또는 Firebase Analytics)를 통합하여 기록된 이벤트 트랜잭션을 비동기적으로 푸시합니다. (사용자 요청에 따라 제외)
