# Item & Booster System Checklist

Checklist for player-activated boosters that modify board state outside the normal tap-group flow (Bomb, H-Rocket, V-Rocket), their inventory management, UI/UX, and server-side tracking.

## 1. Item Definitions & Board Interaction (MVP)
- [x] **Bomb (3x3 Area)**: Clear all cells (Basic, Core, Obstacle) in a 3x3 grid centered on the target cell. Strip one protector layer on affected cells.
  - Reference: [BombEffect.cs](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/client/project-flood/Assets/Scripts/InGame/Items/BombEffect.cs)
- [x] **Horizontal Rocket (Row Sweep)**: Sweeps left to right. Skips Void positions (skip-continue). Destroys Basic/Core/Protector (strips one layer). Stops immediately at first Obstacle cell (destroy-and-stop).
  - Reference: [HRocketEffect.cs](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/client/project-flood/Assets/Scripts/InGame/Items/HRocketEffect.cs)
- [x] **Vertical Rocket (Column Sweep)**: Sweeps top to bottom. Skips Void positions (skip-continue). Destroys Basic/Core/Protector. Stops immediately at first Obstacle cell (destroy-and-stop).
  - Reference: [VRocketEffect.cs](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/client/project-flood/Assets/Scripts/InGame/Items/VRocketEffect.cs)
- [x] **No Turn Consumption**: Item usages do not consume turns. Locked and unusable when remaining turns reach 0.
  - Reference: [InGameController.cs:L101](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/client/project-flood/Assets/Scripts/InGame/Controller/InGameController.cs#L101)
- [x] **Dev Mode (Infinite Inventory)**: Inspector toggle `IsDevMode` on `InGameController` bypasses consumption and shows "∞" badge.
  - Reference: [ItemInventory.cs](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/client/project-flood/Assets/Scripts/InGame/Items/ItemInventory.cs)

## 2. In-Game UI/UX Flow (MVP)
- [x] **Item Tray View Layout**: Bottom layout displaying available item icons and counts. Greyed out if count = 0.
  - Reference: [ItemTrayView.cs](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/client/project-flood/Assets/Scripts/InGame/View/ItemTrayView.cs)
- [x] **Targeting Use Phase**: Tap slot -> Glow slot -> Pulse board cells. Tapping board cell executes item instantly. Tap slot again or tap outside to cancel.
  - Reference: [ItemManager.cs](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/client/project-flood/Assets/Scripts/InGame/Items/ItemManager.cs)
- [/] **VFX Animation Feedback**: Particle/VFX animations for Bomb blast and Rocket sweeps. Board updates cell sprites and drops remaining cells via gravity post-blast.
  - Status: Basic animations implemented in [BoardView.cs](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/client/project-flood/Assets/Scripts/InGame/View/BoardView.cs). Needs hyper-casual grade vibrant juice/polish.

## 3. Phase N (Monetization & Inventory Progression)
- [ ] **Server-Backed Item Inventory**: Persistence of item inventory counts per player in server DB, synchronized via client-login handshake.
- [ ] **In-Game Item Shop purchase**: Add UI button to item slot when count = 0 to instantly buy boosters with gold (1 Bomb = 100 Gold).
- [ ] **Pre-game Boosters selection**: Allow selecting boosters (e.g., "+3 starting turns", "double score") on the Lobby StageInfoPopup before entering the scene.
- [ ] **Streak Boosters (Win Streaks)**: Win consecutive stages to get free starting boosters placed on the board (e.g., spawn random Rocket at stage start).
- [ ] **Vibrant Item VFX/SFX**: Punchy sound effects (whoosh, explosion, metal hit) and particle sparks matching the visual palette of the active theme chapter.
