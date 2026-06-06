# Item & Booster System Checklist

Checklist for player-activated boosters that modify board state outside the normal tap-group flow (Bomb, H-Rocket, ColorSweep, RowShift, CellSwap), their inventory management, UI/UX, and server-side tracking.

## 1. Item Definitions & Board Interaction (MVP)
- [x] **Bomb (3x3 Area)**: Clear all cells (Basic, Core, Obstacle) in a 3x3 grid centered on the target cell. Strip one protector layer on affected cells.
  - Reference: [BombEffect.cs](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/client/project-flood/Assets/Scripts/InGame/Items/BombEffect.cs)
- [x] **Horizontal Rocket (Row Sweep)**: Sweeps left to right. Skips Void positions (skip-continue). Destroys Basic/Core/Protector (strips one layer). Stops immediately at first Obstacle cell (destroy-and-stop).
  - Reference: [HRocketEffect.cs](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/client/project-flood/Assets/Scripts/InGame/Items/HRocketEffect.cs)
- [ ] **ColorSweep**: Removes all cells on the board matching the color ID of the tapped cell. Obstacles unaffected (no color ID). Protector cells take DirectHit (strip one layer, not removed). See ADR-007.
  - Reference: [ColorSweepEffect.cs](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/client/project-flood/Assets/Scripts/InGame/Items/ColorSweepEffect.cs)
- [ ] **RowShift (Horizontal Compaction)**: Swipe gesture (left or right) on board. Packs all cells in each row toward the swipe direction, eliminating empty slots. Void positions act as hard boundaries per row segment. Minimum swipe distance threshold required to prevent accidental activation. GravitySystem runs after. See ADR-007.
  - Reference: [RowShiftEffect.cs](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/client/project-flood/Assets/Scripts/InGame/Items/RowShiftEffect.cs)
- [ ] **CellSwap (Two-Cell Position Swap)**: Two-tap flow — tap first cell (highlighted), tap second cell → positions swapped. GravitySystem runs after. Simple UX; intended to be given in high quantities. See ADR-007.
  - Reference: [CellSwapEffect.cs](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/client/project-flood/Assets/Scripts/InGame/Items/CellSwapEffect.cs)
- [x] **No Turn Consumption**: Item usages do not consume turns. Locked and unusable when remaining turns reach 0.
  - Reference: [InGameController.cs:L101](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/client/project-flood/Assets/Scripts/InGame/Controller/InGameController.cs#L101)
- [x] **Dev Mode (Infinite Inventory)**: Inspector toggle `IsDevMode` on `InGameController` bypasses consumption and shows "∞" badge.
  - Reference: [ItemInventory.cs](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/client/project-flood/Assets/Scripts/InGame/Items/ItemInventory.cs)

## 2. In-Game UI/UX Flow (MVP)
- [x] **Item Tray View Layout**: Bottom layout displaying available item icons and counts. Greyed out if count = 0. Now shows 5 slots.
  - Reference: [ItemTrayView.cs](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/client/project-flood/Assets/Scripts/InGame/View/ItemTrayView.cs)
- [x] **Targeting Use Phase (tap-to-target)**: Tap slot -> Glow slot -> Pulse board cells. Tapping board cell executes item instantly. Tap slot again or tap outside to cancel. Applies to Bomb, H-Rocket, ColorSweep.
  - Reference: [ItemManager.cs](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/client/project-flood/Assets/Scripts/InGame/Items/ItemManager.cs)
- [ ] **RowShift Swipe Phase**: When RowShift selected, board captures horizontal swipe instead of tap. Minimum swipe distance threshold (configurable constant). Short/vertical swipes ignored; player stays in Use Phase.
  - Reference: [ItemManager.cs](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/client/project-flood/Assets/Scripts/InGame/Items/ItemManager.cs)
- [ ] **CellSwap Two-Tap Phase**: FirstCellSelected state — first valid tap highlights source cell; second valid tap executes swap. Tap source cell again to deselect. Tap slot or outside board to cancel.
  - Reference: [ItemManager.cs](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/client/project-flood/Assets/Scripts/InGame/Items/ItemManager.cs)
- [/] **VFX Animation Feedback**: Particle/VFX animations for Bomb blast, Rocket sweep, ColorSweep color-wave, RowShift slide, CellSwap position exchange. Board updates cell sprites and drops remaining cells via gravity post-effect.
  - Status: Basic animations implemented in [BoardView.cs](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/client/project-flood/Assets/Scripts/InGame/View/BoardView.cs). New items need VFX. Needs hyper-casual grade vibrant juice/polish overall.

## 3. Phase N (Monetization & Inventory Progression)
- [ ] **Server-Backed Item Inventory**: Persistence of item inventory counts per player in server DB, synchronized via client-login handshake.
- [ ] **In-Game Item Shop purchase**: Add UI button to item slot when count = 0 to instantly buy boosters with gold (1 Bomb = 100 Gold).
- [ ] **Pre-game Boosters selection**: Allow selecting boosters (e.g., "+3 starting turns", "double score") on the Lobby StageInfoPopup before entering the scene.
- [ ] **Streak Boosters (Win Streaks)**: Win consecutive stages to get free starting boosters placed on the board (e.g., spawn random Rocket at stage start).
- [ ] **Vibrant Item VFX/SFX**: Punchy sound effects (whoosh, explosion, metal hit) and particle sparks matching the visual palette of the active theme chapter.
