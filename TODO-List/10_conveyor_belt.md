# Conveyor Belt Gimmick Checklist

Scope: row-based `ConveyorLeft` and `ConveyorRight` board-floor gimmick. Full spec: [docs/design/conveyor-belt-design.md](../docs/design/conveyor-belt-design.md).

## 1. Data and Rule Spec

- [ ] **Finalize `conveyor_data` encoding**: use row-segment format `row:start_col-end_col:L/R`, or document any compatible parser migration from the current path-style prototype.
- [ ] **Update `shared/datas/stage/AGENTS.md`**: document `portal_data` and `conveyor_data` columns, conveyor segment constraints, and rotation behavior.
- [ ] **Register optional dynamic resources**: add `conveyor_left`, `conveyor_right`, `conveyor_marker_left`, `conveyor_marker_right` to `shared/datas/common/dynamic_resource.csv` after sprite files exist.
- [ ] **Update `shared/datas/common/AGENTS.md`**: document conveyor dynamic resource keys once registered.
- [ ] **Run data generation**: run `npm run gen:info` after CSV changes; never edit generated data directly.

## 2. Unity Rule Engine

- [ ] **Replace prototype conveyor movement**: move current `InGameController.ShiftConveyors()` behavior into a dedicated conveyor rule system that understands row segments and direction.
- [ ] **Parse row-segment metadata**: update `StageLoader` and `BoardState` so conveyors are floor metadata, not `CellType` occupants.
- [ ] **Fix turn order**: apply initial gravity stabilization, then conveyors, then gravity again.
- [ ] **Audit action flows**: verify normal tap, Bomb, HRocket, ColorSweep, RowShift, CellSwap, starting boosters, and special-cell taps all follow the intended conveyor policy.
- [ ] **Support simultaneous segments**: resolve all conveyor segments from the pre-conveyor snapshot before mutating the board.
- [ ] **Support wrap movement**: left/right segment end cells wrap to the opposite end.
- [ ] **Move obstacles**: ensure current `Obstacle` occupants move with conveyor slots.
- [ ] **Rotate conveyor metadata**: 180-degree board rotation must rotate segment coordinates and reverse `Left <-> Right`.
- [ ] **Add Unity tests**: cover segment movement, void boundaries, multiple segments, obstacle movement, null-slot movement, and 180-degree rotation.

## 3. Unity Visuals and UX

- [ ] **Add floor rendering**: extend `BoardBackground` or add a floor overlay layer below `CellView` occupants.
- [ ] **Load dynamic resource sprites**: use `dynamic_resource.csv` keys when present.
- [ ] **Fallback rendering**: draw row highlight/arrow markers when conveyor sprites are missing.
- [ ] **Avoid global spacing change**: keep current board density unless later UX testing proves conveyor direction is unreadable.
- [ ] **Animate conveyor shifts**: reuse or adapt existing slide/swap animation only after checking wrap visuals.
- [ ] **Handle wrap animation**: avoid animating a wrapped cell across the entire row; use instant reposition plus short edge slide.
- [ ] **Add SFX/VFX hook**: optional conveyor tick/slide feedback after base logic is stable.

## 4. Stage Editor

- [ ] **Add conveyor brush/mode**: allow painting `ConveyorLeft`, `ConveyorRight`, and erasing conveyor floor metadata without changing the occupant cell.
- [ ] **Render conveyor floor**: show conveyor direction under cells and fallback edge arrows/highlights.
- [ ] **Validate segments**: block length 1, mixed direction, out-of-bounds, overlap, non-horizontal, `Void` inclusion, and invalid boundary cases.
- [ ] **Save/load metadata**: preserve conveyor data through CSV read/write.
- [ ] **Rotate metadata in playtest**: editor 180-degree rotation must transform coordinates and reverse direction.
- [ ] **Update playtest simulation**: conveyor movement must run after gravity stabilization and before second gravity stabilization.
- [ ] **Update solver**: use the same conveyor rule as playtest.
- [ ] **Update validator**: replay `verified_solution` with conveyor and rotation parity.
- [ ] **Update auto-generator**: treat pre-placed `Void` and conveyor metadata as fixed constraints before board fill and candidate scoring.
- [ ] **Update worker path**: ensure generator web workers receive and apply `conveyor_data`.

## 5. Documentation and Tutorial

- [ ] **Update game design**: replace the old four-direction conveyor description with the row-only left/right spec.
- [ ] **Update stage editor design**: document conveyor brush, validation, playtest, and generator behavior.
- [ ] **Update FTUE/tutorial data plan**: add a future `GimmickAppear` tutorial entry for first conveyor stage.
- [ ] **Update chapter theme plan**: keep conveyor as Chapter 8 volcano gimmick unless stage order changes.
- [ ] **Update client AGENTS docs**: document new runtime classes, symbols, and visual resources after implementation.
- [ ] **Update stage editor AGENTS docs**: document new types/functions/components after implementation.

## 6. Verification

- [ ] **Unity gameplay smoke test**: play a stage with one left segment, one right segment, obstacles, and void boundaries.
- [ ] **Rotation parity test**: compare a board before/after 180-degree rotation and confirm direction reversal.
- [ ] **Stage editor playtest parity**: replay the same move sequence in editor and Unity and compare final boards.
- [ ] **Generator fixed-layout test**: generate from a board with pre-placed void/conveyor rows and verify constraints remain intact.
- [ ] **Missing-sprite fallback test**: remove conveyor resource keys locally and verify fallback rendering works.
- [ ] **Export validation test**: invalid conveyor layouts must block export; valid layouts must pass.
