# Platform Auth Reference

Source of truth: `platform/docs/refs/auth.md`

## JWT Claims
| claim | type | description |
|-------|------|-------------|
| `sub` | string | platform_pid (24-char public ID) |
| `aud` | string | `platform-games` |
| `iss` | string | platform auth URL |

## Endpoints (platform-auth service)
| method | path | description |
|--------|------|-------------|
| GET | `/.well-known/jwks.json` | Public key set for offline JWT validation |
| POST | `/auth/guest` | Guest login — returns access + refresh token |
| POST | `/auth/refresh` | Refresh token exchange |
| POST | `/auth/logout` | Invalidate refresh token |

## Auth Mode
- `AUTH_USE_MOCK=true` → `MockAuthenticationHandler` (dev only, no JWKS fetch)
- `AUTH_USE_MOCK=false` → `JwtPublicKeyCache` fetches JWKS on startup + caches keys
