# Lobby, Stage Progression & Meta Systems Checklist

Checklist for the main Lobby screen, stage roadmap view, progression mechanics, and social/ranking leaderboard integration.

## 1. Main Lobby & Bottom Nav (MVP)
- [x] **Bottom Navigation Tabs**: Tabs for navigation (Home, Rankings, Settings).
  - Reference: [BottomNavBarView.cs](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/client/project-flood/Assets/Scripts/OutGame/Lobby/BottomNavBarView.cs)
- [x] **Lobby View Management**: Panel switching and tab refresh triggers.
  - Reference: [LobbyView.cs](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/client/project-flood/Assets/Scripts/OutGame/Lobby/LobbyView.cs)
- [x] **Top Header Info**: Shows player nickname/avatar and current soft currency (Gold).
  - Reference: [HeaderView.cs](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/client/project-flood/Assets/Scripts/OutGame/Lobby/HeaderView.cs)

## 2. Stage Progression & Roadmap (MVP)
- [x] **S-shape Stage Roadmap**: Renders stages dynamically in a portrait-scrolled zigzag format with Catmull-Rom curved connectors.
  - Reference: [HomeTabView.cs](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/client/project-flood/Assets/Scripts/OutGame/Lobby/HomeTabView.cs)
- [x] **Stage Node View States**: Displays correct visual state per stage (Locked, Unlocked, Current indicator, achieved Stars count 0-3).
  - Reference: [StageNodeView.cs](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/client/project-flood/Assets/Scripts/OutGame/Lobby/StageNodeView.cs)
- [x] **Stage Select Info Popup**: Displays details of the stage (best stars, turn limits) before playing.
  - Reference: [StageInfoPopupView.cs](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/client/project-flood/Assets/Scripts/OutGame/Lobby/StageInfoPopupView.cs)
- [x] **Local Sequential Unlocking**: Unlocks stage N+1 locally after clearing stage N.
  - Reference: [PlayerProgressService.cs:L61](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/client/project-flood/Assets/Scripts/Services/PlayerProgressService.cs#L61)

## 3. Social & Rankings (MVP)
- [x] **Global Ranking API**: Fetch paged leaderboards for total earned stars or highest stage cleared. Rebuilds Redis indices from DB aggregates.
  - Reference: [RankingController.cs](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/server/src/ProjectFlood.API/Controllers/RankingController.cs) and [RankingService.cs](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/server/src/ProjectFlood.Application/Ranking/RankingService.cs)
- [x] **Ranking Tab View**: Paged global leaderboards (Stars, Max Stage) with footer showing current user's profile ranking card.
  - Reference: [RankingTabView.cs](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/client/project-flood/Assets/Scripts/OutGame/Lobby/RankingTabView.cs)
- [x] **Stage-specific rankings**: Best turns ranking per stage (competition style: mine vs others). Rebuilt if Redis is lost.
  - Reference: [StageAttemptService.cs:L109](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/server/src/ProjectFlood.Application/Stage/StageAttemptService.cs#L109)

- [x] **Chapter Groupings & Visual Themes**: Group stages into chapters. Update environment sprites/themes via `theme_key` using shader-based background swaps. (Completed)

## 4. Lobby & Progression Expansion (Active Scope)
- [ ] **Chapter Milestone Chests**: Render chests on the Lobby Roadmap at the end of each Chapter. Unlocked and claimable (once) only when all stages in the chapter have 3 Stars.
  - [ ] Implement server verification during reward claim: check `user_stage_progress` for 3-star clears.
- [ ] **Player Custom Profiles**: Allow players to change display names, and select/unlock unique profile avatars via gold or achievements.
  - [ ] Implement backend profile update APIs and DB column for active avatar ID.
- [ ] **Dynamic Ranking List Virtualization**: Optimize the Unity ranking UI with scroll virtualization to handle lists with thousands of entries efficiently.
