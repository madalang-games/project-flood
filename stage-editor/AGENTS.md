# stage-editor — Next.js Stage Editor Service

Standalone development tool. Reads/writes `shared/datas/stage/stage.csv` and `shared/datas/common/color_palette.csv` via Next.js API routes. See ADR-005.

## Nav
| path | role |
|------|------|
| `src/app/page.tsx` | Main editor UI entry point |
| `src/app/api/stages/` | Stage CRUD API routes |
| `src/app/api/palette/` | Color palette API route |
| `src/components/` | React UI components |
| `src/lib/` | CSV parser, CTM encoder, game-rules TS port, validator |
| `src/types/` | TypeScript type definitions |

## Rules
- Run from `project-flood/` root; CSV paths resolve relative to it
- `lib/game-rules.ts` must mirror C# rule engine — update both when rules change
- NEVER write to `shared/datas/` manually from outside this service during editor session
- `_` prefix files/dirs skipped per project convention
- NEW_DIR: create `AGENTS.md` for it + update Nav above
