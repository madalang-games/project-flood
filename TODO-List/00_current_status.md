# Current Project Status & Roadmap

Current Phase: **MVP Integration & Release Stabilization**  
Focus: Connecting client scenes to server endpoints, integrating ads, and preparing for soft launch.

## 1. Feature Area Checklists
Refer to the area-specific checklists for detailed requirements, current progress status, and planned Phase N improvements:

- [ ] **[01_gameplay_ingame.md](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/TODO-List/01_gameplay_ingame.md)**: Core Puzzle Rules & Game Loop
- [ ] **[02_items_boosters.md](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/TODO-List/02_items_boosters.md)**: Bomb & Rocket Boosters
- [ ] **[03_stamina_economy.md](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/TODO-List/03_stamina_economy.md)**: Stamina Gate & Gold Economy
- [ ] **[04_lobby_progression.md](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/TODO-List/04_lobby_progression.md)**: Lobby Roadmap & Leaderboards
- [ ] **[05_stage_editor.md](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/TODO-List/05_stage_editor.md)**: Web Stage Editor & Solver
- [ ] **[06_auth_infrastructure.md](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/TODO-List/06_auth_infrastructure.md)**: Platform Authentication & MySQL/Redis Stack
- [ ] **[07_ads_monetization.md](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/TODO-List/07_ads_monetization.md)**: AdMob rewarded/interstitial ads
- [ ] **[08_polish_ux_sfx.md](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/TODO-List/08_polish_ux_sfx.md)**: Visual juice, VFX particles, and Audio SFX/BGM

---

## 2. In Progress (Active Sprint)
- [/] **Stamina Integration**: Adding Stamina Life & regen timer UI to client [HeaderView.cs](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/client/project-flood/Assets/Scripts/OutGame/Lobby/HeaderView.cs) and validating client entrance.
- [/] **Leaderboard Integration**: Connecting client [RankingTabView.cs](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/client/project-flood/Assets/Scripts/OutGame/Lobby/RankingTabView.cs) to paged server API routes.
- [/] **Platform Auth Integration**: Transitioning client [AuthService.cs](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/client/project-flood/Assets/Scripts/Services/AuthService.cs) to request real stateless platform tokens.

---

## 3. Recently Completed (Done)
- [x] Web Stage Editor playtest mode & TS solver integration.
- [x] server-side AdSsvCallbackController for ad verify notifications.
- [x] verified_solution recording and validator export checks.
- [x] C# rules engine in Unity (matching, gravity, 180 board rotation, protector strip, core cells).
- [x] Item system C# core models (Bomb, HRocket, VRocket, Dev mode).
- [x] EventLog audit records for transactional APIs.
- [x] Docker-compose MySQL/Redis development environments.
