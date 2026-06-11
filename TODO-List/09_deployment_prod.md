# 프로덕션 배포 체크리스트

배포 대상: `pixelpop.mooo.com` → `project-flood-api:8080` (madalang-net)  
서버: `madalang.mooo.com` (SSH: `ssh -p 2222 -i prod.pem sang@madalang.mooo.com`)

---

## Phase 1 — DNS & SSL (플랫폼 측)

> ⚠️ 순서 필수: **cert 발급 → deploy.bat (platform) → 자동 nginx reload**
> cert 없이 deploy.bat으로 platform 배포 후 nginx 컨테이너가 재시작되면 nginx startup fail.

- [x] **DNS**: `pixelpop.mooo.com` A레코드 → 서버 IP 등록 후 전파 확인
- [x] **SSL 인증서 발급**: `add-cert.sh pixelpop.mooo.com` 실행 완료
- [x] **Platform 배포 (conf.d 포함)**: `deploy.bat` → Platform 선택, nginx reload 완료
- [ ] **검증**: `curl https://pixelpop.mooo.com/api/bootstrap/config` → project-flood-api 응답 확인
  - (project-flood 컨테이너 올라온 후 확인 — Phase 4 이후)

---

## Phase 2 — 환경변수 / Secrets

- [ ] **`.env.prod` 생성**: `project-flood/.env.prod.example` 기반으로 서버에서 작성
  ```bash
  # 서버에서 실행
  cp project-flood/.env.prod.example project-flood/.env.prod
  # 또는 secrets/project-flood/.env.prod 경로에 주입
  ```
- [ ] **DB 패스워드 설정**: `DB_PASSWORD`, `DB_ROOT_PASSWORD` → 강력한 실제 값으로 교체
- [ ] **DB_USER**: secrets에서 주입 예정 (change_me 그대로 두지 말 것)
- [ ] **JWT 검증**: `JWT_AUTHORITY=http://platform-auth:8080` 이 platform-auth의 실제 `AUTH_ISSUER`와 일치하는지 확인
  - platform `.env.prod.example` 기준 `AUTH_ISSUER=http://platform-auth:8080` → 일치 ✅
  - 만약 platform prod에서 AUTH_ISSUER를 공개 URL(`https://auth.madalang.mooo.com`)로 설정했다면 `JWT_ISSUER` 반드시 수정
- [ ] **클라이언트 버전 동기화**: `APP_ALLOWED_CLIENT_VERSION`, `APP_REQUIRED_CLIENT_VERSION` → 실제 APK versionName으로 설정
- [ ] **AdMob 모드 확인**: `AD_REWARD_VERIFY_MODE=mock` — AdMob 프로덕션 연동 전까지 의도적 유지
- [ ] **인증 모드 확인**: `AUTH_USE_MOCK=false` — 반드시 false

---

## Phase 3 — DB 초기화

- [ ] **madalang-net 네트워크 존재 확인**: 플랫폼 스택이 올라가 있으면 이미 존재
  ```bash
  docker network ls | grep madalang-net
  ```
- [ ] **MySQL 컨테이너 기동 후 마이그레이션 적용**: 컨테이너 올린 뒤 (api 기동 전) 마이그레이션 실행
  ```bash
  # 마이그레이션 파일: server/db/migrations/ (최신: 2026-06-09)
  # project-flood-mysql 컨테이너가 healthy 상태가 된 후 실행
  for f in $(ls project-flood/server/db/migrations/*.sql | sort); do
    docker exec -i project-flood-mysql mysql -u root -p<ROOT_PW> projectflood_db < "$f"
    echo "Applied: $f"
  done
  ```
- [ ] **마이그레이션 검증**: `SHOW TABLES;`로 테이블 목록 확인

---

## Phase 4 — 컨테이너 배포

- [ ] **`secrets/project-flood/.env.prod` 준비**: deploy.bat이 자동으로 `project-flood/.env.prod`로 주입
- [ ] **deploy.bat 실행**: `3` (Project-Flood) 또는 `4` (All) 선택
  - `server/`, `shared/`, `docker-compose*.yml` 패키징 → SCP → `deploy-remote.sh` 실행
  - `docker compose --env-file .env.prod -f docker-compose.yml -f docker-compose.prod.yml up -d --build`
- [ ] **컨테이너 상태 확인**: 서버에서 `sudo docker ps | grep project-flood` — api, mysql, redis 3개 Running
- [ ] **API 로그 확인**: `sudo docker logs project-flood-api` — startup 에러 없음 확인
- [ ] **로컬 헬스 체크**: `curl http://localhost:20201/api/bootstrap/config` → JSON 응답
- [ ] **공개 헬스 체크**: `curl https://pixelpop.mooo.com/api/bootstrap/config` → JSON 응답

---

## Phase 5 — Unity 클라이언트 빌드

- [ ] **서버 URL 확인**: `NetworkService` (또는 ServerConfig)에서 API 베이스 URL이 `https://pixelpop.mooo.com` 으로 설정되어 있는지 확인
- [ ] **Android 릴리즈 키스토어 설정**: Player Settings → Publishing Settings → keystore 설정
- [ ] **버전 맞춤**: `versionName`, `versionCode` 설정 → .env.prod의 `APP_ALLOWED_CLIENT_VERSION`과 동기화
- [ ] **Release APK/AAB 빌드**: Build Settings → Android → Release 빌드
- [ ] **Google Play Console 등록**: 앱 스토어 리스팅 (앱 이름, 설명, 스크린샷 등)

---

## Phase 6 — AdMob 프로덕션 연동 (런칭 이후)

> AdMob은 실제 프로덕션 런칭 이후 별도 스프린트로 진행. 런칭 시 `AD_REWARD_VERIFY_MODE=mock` 유지.

- [ ] AdMob 콘솔에서 프로덕션 앱 등록 → App ID 발급
- [ ] Unity `GoogleMobileAds Settings`에서 프로덕션 App ID 설정
- [ ] 각 광고 유닛(Rewarded: STAMINA_LIFE, STAGE_REVIVE, DOUBLE_REWARD_STAGE_CLEAR / Interstitial) 프로덕션 Ad Unit ID로 교체
- [ ] AdMob SSV 콜백 URL 등록: `https://pixelpop.mooo.com/api/ad/ssv-callback`
- [ ] `.env.prod`에서 `AD_REWARD_VERIFY_MODE=ssv` 로 변경 후 API 컨테이너 재시작

---

## 배포 준비 상태 요약

| 항목 | 상태 | 비고 |
|------|------|------|
| 서버 코드 (MVP) | ✅ 완료 | 모든 MVP 기능 구현 완료 |
| docker-compose 파일 | ✅ 완료 | base/dev/prod 모두 존재 |
| .env.prod.example | ✅ 완료 | 모든 필수 env 포함 |
| Dockerfile | ✅ 완료 | multi-stage build |
| DB 마이그레이션 파일 | ✅ 완료 | 최신 2026-06-09 |
| madalang-net 통합 | ✅ 완료 | docker-compose.yml에 external network 선언 |
| nginx conf (pixelpop) | ✅ 완료 | 방금 생성 |
| DNS 등록 | ✅ 완료 | pixelpop.mooo.com A레코드 등록됨 |
| SSL 인증서 | ✅ 완료 | add-cert.sh로 발급 완료 |
| .env.prod (서버) | ❌ 미완 | secrets에서 생성 필요 |
| DB 마이그레이션 적용 | ❌ 미완 | prod DB에 최초 적용 필요 |
| Unity 클라이언트 빌드 | ❌ 미완 | 릴리즈 키스토어 + 빌드 필요 |
| AdMob 프로덕션 | ⏸️ 런칭 이후 | mock 모드로 런칭 |
| VFX/SFX 폴리싱 | ⏸️ 진행 중 | 서버 배포 블로커 아님 |
