# Current Project Status & Roadmap

Current Phase: **MVP Client-Server Integration**  
Focus: Wiring client scenes to live server APIs, stabilizing stamina gate, and real auth handshake.

---

## 1. Feature Area Checklists
| file | area | MVP status |
|------|------|-----------|
| [01_gameplay_ingame.md](01_gameplay_ingame.md) | Core Puzzle Rules & Game Loop | ✅ MVP done |
| [02_items_boosters.md](02_items_boosters.md) | All 5 Items (Bomb/HRocket/ColorSweep/RowShift/CellSwap) | ✅ Logic done — VFX needs polish |
| [03_stamina_economy.md](03_stamina_economy.md) | Stamina Gate & Gold Economy | ✅ MVP done |
| [04_lobby_progression.md](04_lobby_progression.md) | Lobby Roadmap & Leaderboards | ✅ MVP done |
| [05_stage_editor.md](05_stage_editor.md) | Web Stage Editor & Solver | ✅ MVP done |
| [06_auth_infrastructure.md](06_auth_infrastructure.md) | Platform Auth & DB/Redis Stack | ✅ MVP done |
| [07_ads_monetization.md](07_ads_monetization.md) | AdMob Rewarded/Interstitial | [/] Partial — rewarded verify overlay missing |
| [08_polish_ux_sfx.md](08_polish_ux_sfx.md) | Visual Polish, VFX, Audio | [/] Basic done — juice/SFX not started |

---

## 2. MVP Blockers (must fix before soft launch)

These are the remaining gaps that block a shippable MVP:

### A. Rewarded Ad Verify UX [07]
- Client sends nonce; server receives SSV callback — flow is wired
- Missing: loading blocker overlay while SSV callback settles (user can tap away prematurely)
- **Next action**: Show modal spinner after ad closes; poll or wait for server confirmation before dismissing

### ~~B. Stamina Gate E2E~~ ✅ Done
### ~~C. Gold Server Sync~~ ✅ Done
### ~~D. Unity Client Auth~~ ✅ Done

---

## 3. In Progress (Active Sprint)
- [/] **Rewarded Ad Verify UX**: Show loading spinner while SSV callback settles after ad closes; prevent premature dismiss.

---

## 4. Recently Completed
- [x] **Stamina Gate E2E**: `LobbyView` now calls `StaminaApiService.FetchStamina()` on lobby load; `HeaderView` renders live data; `StageInfoPopupView` gates entry.
- [x] **Gold Server Sync**: `CurrencyApiService` (GET + POST /spend); `LobbyView` fetches gold on load; stage clear reconciles from server response; continue spend deducts server-side.
- [x] **Auth Integration**: `AuthService` already wired to `/api/auth/guest` + JWT Bearer — confirmed done.
- [x] **All 5 Items implemented**: ColorSweep, RowShift (swipe phase), CellSwap (two-tap phase), Bomb, HRocket — full logic + InGameController input routing.
- [x] **VRocket removed**: Replaced by ColorSweep/RowShift/CellSwap per revised item design.
- [x] **StaminaPopupView**: Stamina popup with heart count, countdown timer, and Watch Ad button.
- [x] **StageInfoPopupView stamina check**: Entry gate checks `StaminaApiService` before allowing play.
- [x] **Lobby AGENTS.md updated**: HeaderView stamina fields, StaminaPopupView symbols documented.
- [x] **Web Stage Editor playtest mode & TS solver integration**.
- [x] **server-side AdSsvCallbackController for ad verify notifications**.
- [x] **C# rules engine in Unity (matching, gravity, 180 board rotation, protector strip, core cells)**.
- [x] **EventLog audit records for transactional APIs**.
- [x] **Docker-compose MySQL/Redis development environments**.
