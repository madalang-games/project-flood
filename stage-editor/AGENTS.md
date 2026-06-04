# stage-editor — Next.js Stage Editor Service

Standalone development tool. Reads/writes `shared/datas/stage/stage.csv` and `shared/datas/common/color_palette.csv` via Next.js API routes. See ADR-005.

## Nav
| path | role |
|------|------|
| `src/app/page.tsx` | Main editor UI — all state, handlers, layout |
| `src/app/layout.tsx` | Root layout + Tailwind CSS |
| `src/app/api/stages/route.ts` | GET all stages, POST new stage |
| `src/app/api/generator-defaults/route.ts` | GET generator defaults from `template.ini [stage-editor-generator]` |
| `src/app/api/stages/[id]/route.ts` | GET, PUT, DELETE by stage_id |
| `src/app/api/palette/route.ts` | GET all 16 palette colors |
| `src/components/StageList.tsx` | Left sidebar — stage list + new/delete |
| `src/components/BoardEditor.tsx` | Grid canvas — paint / erase / playtest tap |
| `src/components/CellInspector.tsx` | Right panel — brush + selected cell editor |
| `src/components/MetadataPanel.tsx` | Stage metadata fields (size, turns, difficulty, ratios) |
| `src/components/PlaytestPanel.tsx` | Playtest controls, recording, validate, export, save |
| `src/components/GeneratorPanel.tsx` | Generator mode — random board fill from settings (colors, obstacles, protectors, cores) |
| `src/lib/csv.ts` | CSV read/write (4-row header format) |
| `src/lib/ctm.ts` | CTM hex encode/decode per cell (ADR-003) |
| `src/lib/game-rules.ts` | TS port: BFS, removal, gravity, clear evaluator |
| `src/lib/validator.ts` | Export validation — solution replay + warnings |
| `src/types/stage.ts` | StageRow, CellData, CellType, PaletteColor, BrushSettings, StageMeta |

## Files
| file | class/export | role |
|------|-------------|------|
| `src/components/GeneratorPanel.tsx` | `GeneratorSettings` | colorCount, obstacleCount, protectorCount, protectorLevel, coreCellCount |
| `src/types/stage.ts` | `CellData` | colorId, type, protector, isCore |
| `src/types/stage.ts` | `StageRow` | Raw CSV row shape |
| `src/types/stage.ts` | `StageMeta` | StageRow minus cells/color_ids (edit state) |
| `src/types/stage.ts` | `BrushSettings` | Current paint brush |
| `src/lib/ctm.ts` | `decodeCTM` | 3-char hex → CellData |
| `src/lib/ctm.ts` | `encodeCTM` | CellData → 3-char hex |
| `src/lib/ctm.ts` | `decodeCells` | cells string → CellData[][] |
| `src/lib/ctm.ts` | `encodeCells` | CellData[][] → cells string |
| `src/lib/ctm.ts` | `deriveColorIds` | Auto-derive color_ids from grid |
| `src/lib/csv.ts` | `readStages` | Parse stage.csv → StageRow[] |
| `src/lib/csv.ts` | `writeStages` | StageRow[] → rewrite stage.csv |
| `src/lib/csv.ts` | `readPalette` | Parse color_palette.csv → PaletteColor[] |
| `src/lib/game-rules.ts` | `findGroup` | BFS same-color group from (r,c) |
| `src/lib/game-rules.ts` | `applyRemoval` | Strip protector or remove cells |
| `src/lib/game-rules.ts` | `applyGravity` | Downward column compaction |
| `src/lib/game-rules.ts` | `evaluate` | clearance_ratio + star result |
| `src/lib/validator.ts` | `validate` | Replay solution + core warnings |
| `src/lib/solver.ts` | `autoSolve` | BFS (min-move) up to 5 000 states → greedy fallback; single-cell fallback when no ≥2 groups |
| `src/lib/ini.ts` | `parseIni` | Minimal INI parser — returns `Record<section, Record<key, string>>` |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `Board` | type | `(CellData \| null)[][]` — null = empty post-removal |
| `StarResult` | interface | stars 0–3, clearanceRatio, allCleared |
| `ValidationResult` | interface | hasVerifiedSolution, solutionReplaySucceeds, warnings[], canExport |
| `PROJECT_ROOT` | const | env `PROJECT_ROOT` or `process.cwd()/..` — controls CSV path |

## Rules
- Run `npm install` inside `stage-editor/` then `npm run dev`
- CSV paths resolve via `PROJECT_ROOT` env var (default: `cwd/..` = project-flood root)
- `lib/game-rules.ts` must mirror C# rule engine — update both when rules change
- NEVER write to `shared/datas/` manually from outside this service during editor session
- `_` prefix files/dirs skipped per project convention
- NEW_DIR: create `AGENTS.md` for it + update Nav above

## Cross-refs
- Consumed by: dev-only (no runtime dependency)
- Depends on: `shared/datas/stage/stage.csv`, `shared/datas/common/color_palette.csv`
- Gen output: none (editor writes source CSV directly)
