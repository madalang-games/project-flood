# Platform Auth, DB & Infrastructure Checklist

Checklist for server deployment pipelines, database mappings, Redis state caching, platform JWT authentication, event audit logs, and developer stacks.

## 1. Stateless Authentication & User Identity (MVP)
- [x] **Platform JWT stateless validation**: Validates token signatures using cached JWKS public keys.
  - Reference: [JwtPublicKeyCache.cs](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/server/src/ProjectFlood.Infrastructure/Security/JwtPublicKeyCache.cs)
- [x] **User ID Resolution Middleware**: Matches external JWT PID (`sub`) to internal incremental UID, registering custom claims.
  - Reference: [UserIdResolutionMiddleware.cs](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/server/src/ProjectFlood.API/Middleware/UserIdResolutionMiddleware.cs)
- [x] **Unity Client AuthService Server Integration**: Replace Guest device ID stub logic with real JWT authorization header handshakes to securely sync stats.
  - Status: Done. `AuthService.Initialize()` calls `/api/auth/guest` (or `/api/auth/refresh` if token exists), stores JWT via `SecureTokenStorage`, and calls `NetworkService.SetAuthToken()` to attach `Authorization: Bearer` header on all API calls. Falls back to offline guest if server unreachable in editor.

## 2. Infrastructure & Cache (MVP)
- [x] **Docker Compose Stack**: Single-script compose setup configuration for local MySQL db and Redis caching instances.
  - Reference: [docker-compose.yml](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/docker-compose.yml) and [docker-compose.dev.bat](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/docker-compose.dev.bat)
- [x] **Redis Attempt Cache**: Stores volatile, active stage attempts with TTL timeout values matching configuration limits.
  - Reference: [StageAttemptService.cs:L63](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/server/src/ProjectFlood.Application/Stage/StageAttemptService.cs#L63)
- [x] **DB Schema Generator**: Auto-generates EF mappings and SQL migration scripts dynamically from a single JSON schema definition.
  - Reference: [schema.json](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/server/db/schema.json) and [db_generator.bat](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/tools/db_generator.bat)

## 3. Database & Audit Logs (MVP)
- [x] **EF Core ORM & DB Context**: Maps generated MySQL entities to EF DbContext, forcing snake_case column names and foreign key references.
  - Reference: [AppDbContext.cs (generated)](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/server/generated/scripts/Infrastructure/AppDbContext.g.cs)
- [x] **Event Logs Audit Trail**: Log factories append entries to `event_logs` for critical database modifications (stamina delta, reward claim, stage clear, ads watched).
  - Reference: [EventLogFactory.cs (generated)](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/server/generated/scripts/Application/EventLogFactory.g.cs)

## 4. Auth & API Expansion (Active Scope)
- [ ] **Platform OAuth API wiring**: Add sign-in providers (Google Play Games, Apple Game Center) to authenticate guest accounts to permanent social profiles on the server.
- [ ] **Rate Limiting Middleware**: Expand rate-limiting policies on transactional stage endpoints (attempts start, clear, reward claims) to prevent client cheat script exploits.

## 5. Excluded Scope (Phase 2+)
- [ ] **Redis Cluster & DB Failover**: Configure cluster topologies, replica setups, and auto-failover strategies to guarantee high availability in production deployments. (Excluded per user request)
- [ ] **Analytics Event Dispatchers**: Integrate analytics collectors (e.g. BigQuery or Firebase Analytics) to push logged event transactions asynchronously for business intelligence dashboarding. (Excluded per user request)
