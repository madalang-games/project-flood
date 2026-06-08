# Stage Editor & Content Tooling Checklist

Checklist for the standalone Next.js Stage Editor (`tools/stage_editor/`) used to configure stages, record solutions, and validate game rules.

## 1. Web Editor Canvas & UI (MVP)
- [x] **Grid Board Canvas**: Grid sizes up to 16x16 with paint brushes. Left-click paints brush cell state, right-click erases.
  - Reference: [BoardEditor.tsx](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/tools/stage_editor/src/components/BoardEditor.tsx)
- [x] **Cell Brush Inspector**: Configure brush properties: CellType (Basic, Obstacle, Void), Color Palette picker (16 swatches), Protector layers (0-2), and Core designation.
  - Reference: [CellInspector.tsx](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/tools/stage_editor/src/components/CellInspector.tsx)
- [x] **Metadata Panel**: Edit stage fields (Width, Height, Turn limit, Difficulty, custom Star ratios, server reward group ID).
  - Reference: [MetadataPanel.tsx](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/tools/stage_editor/src/components/MetadataPanel.tsx)
- [x] **Stage Generation Mode**: Auto-generate boards based on preset parameters (color count, obstacle density, protector frequency, core count).
  - Reference: [GeneratorPanel.tsx](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/tools/stage_editor/src/components/GeneratorPanel.tsx)

## 2. Playtesting & Export Validation (MVP)
- [x] **Playtest Mode**: Simulated playtest running game rules in TS. Tap matching groups, apply gravity, stripp protectors, and trigger 180° rotation gimmick.
  - Reference: [PlaytestPanel.tsx](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/tools/stage_editor/src/components/PlaytestPanel.tsx) and [game-rules.ts](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/tools/stage_editor/src/lib/game-rules.ts)
- [x] **Solution Recorder**: Record player taps during successful playtest and save as `verified_solution` path string sequence.
  - Reference: [PlaytestPanel.tsx](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/tools/stage_editor/src/components/PlaytestPanel.tsx)
- [x] **Export Validation**: Verify ruleset matching, check if the recorded solution successfully clears the current board state via simulation, and show warnings.
  - Reference: [validator.ts](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/tools/stage_editor/src/lib/validator.ts)
- [x] **Auto-Solver (Solver Integration)**: BFS min-move search (up to 5,000 states) with a greedy fallback to solve stages automatically and verify clearability.
  - Reference: [solver.ts](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/tools/stage_editor/src/lib/solver.ts)

## 3. CSV Integration & Pipelines (MVP)
- [x] **Next.js CRUD API routes**: API endpoints to retrieve, update, delete, and add stage rows directly inside `shared/datas/stage/stage.csv`.
  - Reference: [route.ts](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/tools/stage_editor/src/app/api/stages/route.ts)
- [x] **CTM Hex encoding**: Decodes CTM format from CSV to 2D grid of `CellData`, and encodes it back on write. Auto-derives unique stage colors.
  - Reference: [ctm.ts](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/tools/stage_editor/src/lib/ctm.ts)

## 4. Content Generation & Editor Expansion (Active Scope)
- [ ] **Image-to-Board Auto-Drafting**: Drag-and-drop an image file, pixelate to grid dimensions, map colors to closest LAB color space hex, and correct isolated color nodes automatically.
- [ ] **Advanced Solver Metrics**: Log and display solvability difficulty rating (e.g. state space density, branching factor, minimum moves required) to help balance stage pacing.
- [ ] **Hot-Reloading in Unity Editor**: Create an Editor script in Unity to trigger info_generator pipelines and refresh client stage assets directly when saving in the web editor.
- [ ] **Undo / Redo Canvas Actions**: Support standard keyboard shortcuts (Ctrl+Z / Ctrl+Y) in the web canvas to speed up manual stage design sessions.
