# UI/UX — Auth Flow

## Boot Auth Sequence

```
Boot
  ├── access token valid
  │     → Lobby (authenticated)
  ├── access token expired + refresh token valid
  │     → silent refresh → Lobby
  ├── refresh expired + OAuth credential cached
  │     → silent re-auth → new tokens → Lobby
  ├── no token (first launch / reinstall)
  │     → clientLogin (device UUID) → Lobby (guest)
  └── all failed (OAuth account exists, all tokens expired, re-auth failed)
        → Re-login Required screen  (NEVER auto-fallback to guest)
```

## Re-login Required Screen

Shown only when OAuth account exists but all tokens are expired.

```
┌──────────────────────────┐
│  Session expired          │
│                          │
│  [Re-login]              │  → OAuth flow
│  [Continue as Guest]     │  → explicit choice only
└──────────────────────────┘
```

[Continue as Guest]: must display warning — "Progress linked to your account will not be accessible."

---

## Guest Mode

- No guest indicator shown in Lobby UI.
- Visible only in Account popup: "Guest mode — link an account to keep progress across devices."

### OAuth Link Prompt (once after Chapter 1 clear)

```
┌──────────────────────────┐
│  Save your progress       │
│                          │
│  Link an account to keep  │
│  data after reinstall or  │
│  device change.           │
│                          │
│  [Link with Google]       │
│  [Later]                 │
└──────────────────────────┘
```

- [Later]: no repeat. Manual link via Settings > Account.
- Trigger: Chapter 1 clear event, fires once.

---

## Account Popup (Lobby Header avatar tap)

```
┌──────────────────────┐
│  [Avatar]             │
│  Guest  (or user ID) │
│                      │
│  [Link Account]      │  ← guest only
│  [Switch Account]    │  ← authenticated only
│  [Logout]            │
└──────────────────────┘
```

---

## Account Switching

1. Tap [Switch Account]
2. Confirm dialog: "Switching accounts will replace local data with the new account's data. Your current account data is preserved on the server."
3. OAuth flow
4. On complete → clear all local cache → load new account data from server

---

## Guest → OAuth Link

### New OAuth account (no existing server data)

Guest data migrated server-side to the OAuth account automatically.

### Existing OAuth account (server data already present)

```
┌──────────────────────────────┐
│  This account already has    │
│  existing progress.          │
│                              │
│  [Use existing account data] │  → discard guest data
│  [Migrate guest data]        │  → server-side migration
└──────────────────────────────┘
```

---

## clientLogin

| Case | Behavior |
|------|----------|
| First launch | Generate device UUID → store locally → server clientLogin |
| Re-launch (UUID intact) | clientLogin with same UUID → existing guest data retained |
| Reinstall | New UUID generated → new guest account → previous guest data lost |
| After OAuth link | clientLogin unused → OAuth auth only |

Guest data loss on reinstall: no warning shown (no recovery path without OAuth link).
