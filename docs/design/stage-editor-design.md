# Stage Editor Design

## Overview

Standalone Next.js web app (`stage-editor/`). Reads and writes `shared/datas/stage/stage.csv` and `shared/datas/common/color_palette.csv` via API routes. Development-only tool.

See ADR-005 for architecture rationale.

---

## API Routes

| Method | Route | Description |
|--------|-------|-------------|
| GET | `/api/stages` | All stages (parsed from stage.csv) |
| GET | `/api/stages/[id]` | Single stage by stage_id |
| POST | `/api/stages` | Create new stage (appends row) |
| PUT | `/api/stages/[id]` | Update existing stage |
| DELETE | `/api/stages/[id]` | Delete stage row |
| GET | `/api/palette` | All 16 colors from color_palette.csv |

---

## UI Layout

```
┌─────────────┬──────────────────────────────┬────────────────────┐
│ Stage List  │        Board Editor          │  Cell Inspector    │
│             │  (grid canvas, click=paint)  │  (selected cell)   │
│  [+ New]    │                              │  Type: Basic ▼     │
│  Stage 1    │   [ ][ ][ ][ ]              │  Color: ■ Red ▼    │
│  Stage 2    │   [ ][ ][ ][ ]              │  Protector: 0 ▼    │
│  ...        │   [ ][ ][ ][ ]              │  Core: □           │
│             │   [ ][ ][ ][ ]              │                    │
│             ├──────────────────────────────┤                    │
│             │  Metadata Panel              │                    │
│             │  Width:8 Height:8 Turns:20   │                    │
│             │  Difficulty: Normal ▼        │                    │
│             │  Star1:0.80  Star2:0.90      │                    │
│             ├──────────────────────────────┤                    │
│             │  [▶ Playtest] [⏺ Record]     │                    │
│             │  [✓ Validate] [⬇ Export]     │                    │
└─────────────┴──────────────────────────────┴────────────────────┘
```

---

## Board Editor

- Grid canvas sized to `board_width × board_height` (up to 16×16).
- Left-click cell → apply selected cell inspector settings.
- Right-click cell → reset to empty (Obstacle or clear).
- Board resizes when width/height changes; cells outside new bounds are dropped.
- `color_ids` auto-derived from all unique `C` values in non-Obstacle cells.

### Cell Inspector Options

| Field | Values |
|-------|--------|
| CellType | Basic, Obstacle |
| Color | palette picker (16 colors, shown as colored swatches) |
| Protector | 0 / 1 / 2 |
| Core | on / off |

Cell is encoded as CTM hex on change (see ADR-003).

---

## Playtest Mode

- In-browser simulation using `lib/game-rules.ts` (TypeScript port of C# rule engine).
- Tap cells as in the real game; turn counter decrements.
- Gravity and protector stripping apply after each tap.
- Clear/fail overlay shown on stage end.
- Exit playtest → board returns to pre-playtest state.

### Solution Recorder

- Enable recording before playtest → taps saved as `[row, col]` sequence.
- On successful clear → sequence written to `verified_solution` field as `"row,col;row,col;..."`.
- Existing `verified_solution` is replaced; must re-record after any board change.

---

## Export Validation

Run before writing to CSV. Results shown in UI.

| Check | On Fail |
|-------|---------|
| `verified_solution` exists | Block export |
| `verified_solution` replay succeeds (rule engine replay) | Block export |
| `star1_ratio` achievable given current board | Warn (not block) |
| No Core cell entirely surrounded by Obstacle cells | Warn (not block — cell still tappable per ADR-004) |

---

## File Structure

```
stage-editor/
├── src/
│   ├── app/
│   │   ├── page.tsx               Main editor page
│   │   └── api/
│   │       ├── stages/
│   │       │   ├── route.ts       GET all, POST new
│   │       │   └── [id]/
│   │       │       └── route.ts   GET, PUT, DELETE
│   │       └── palette/
│   │           └── route.ts       GET color palette
│   ├── components/
│   │   ├── StageList.tsx          Left sidebar CRUD list
│   │   ├── BoardEditor.tsx        Grid canvas + paint logic
│   │   ├── CellInspector.tsx      Right panel cell type/color/flags
│   │   ├── MetadataPanel.tsx      Stage metadata fields
│   │   └── PlaytestPanel.tsx      Playtest controls + solution recorder
│   ├── lib/
│   │   ├── csv.ts                 CSV parse/serialize (4-row header format)
│   │   ├── ctm.ts                 CTM hex encode/decode per cell
│   │   ├── game-rules.ts          TS port of BFS, gravity, clear evaluator
│   │   └── validator.ts           Export validation checks
│   └── types/
│       └── stage.ts               StageRow, CellData, CellType, Difficulty TS types
├── next.config.ts
└── package.json
```

---

## CSV Sync Rules

- API server resolves CSV paths relative to `project-flood/` root.
- On write: full file rewritten (4 header rows + all stage rows).
- On read: rows 1–4 are headers; row 5+ are data.
- `stage_id` is assigned sequentially; gaps are not auto-filled.
- NEVER run info_generator during editor session — it overwrites generated output, not source.
