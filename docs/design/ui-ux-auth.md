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

## Guest → OAuth Link Conflict Resolution (Royal Match Style)

When linking a guest account to a social account (Google/Apple) that already has progress on the server, the client must present a comparison card to let the user explicitly choose which progress to keep.

```
┌────────────────────────────────────────────────────────┐
│               Resolve Account Conflict                 │
│                                                        │
│  We found progress on your social account.             │
│  Please select the save file you wish to keep:         │
│                                                        │
│  [ Local (Guest) Save ]     [ Cloud (Social) Save ]    │
│  - Max Stage: Stage 5       - Max Stage: Stage 48      │
│  - Gold: 350                - Gold: 1,420              │
│  - Stars: 12                - Stars: 115               │
│  - Items: 2                 - Items: 15                │
│                                                        │
│  [ Keep Local Save ]        [ Keep Cloud Save ]        │
└────────────────────────────────────────────────────────┘
```

- **Conflict Screen**: Visual panel showing side-by-side comparison of local vs cloud data.
- **Confirmation**: Selecting a save deletes the unselected progress from the server (or marks it as inactive/archived) and updates the active account reference.

---

## API Transactional Rate Limiting

To prevent 어뷰징 (abuse) via cheat scripts calling game endpoints repeatedly:
- **Rate Limit Scope**: Applies to transactional endpoints (Stage Attempt Start, Stage Attempt Clear, Reward Claim, Ad SSV Reward Claim).
- **Rule Policy**: Standard sliding window rate limit of **5 requests per minute** per user ID on transactional endpoints.
- **Handling**: Over-limit requests return `429 Too Many Requests` status with error code `RATE_LIMITED`. Client shows confirm/retry dialog and blocks clicking.

---

## clientLogin

| Case | Behavior |
|------|----------|
| First launch | Generate device UUID → store locally → server clientLogin |
| Re-launch (UUID intact) | clientLogin with same UUID → existing guest data retained |
| Reinstall | New UUID generated → new guest account → previous guest data lost |
| After OAuth link | clientLogin unused → OAuth auth only |

Guest data loss on reinstall: no warning shown (no recovery path without OAuth link).
