# tools - Automation Pipeline

## Nav
| file/dir | role |
|----------|------|
| `config-loader.js` | Loads required `template.ini` + `.env.dev`/`.env.prod` values |
| `info_generator/` | `shared/datas/**/*.csv` -> `*/generated/data/**/*.csv` + `StaticData/*.g.cs` + `IStaticDataService.g.cs` |
| `db_generator/` | `server/db/schema.json` -> DB CREATE/ALTER TABLE + migration SQL + generated EF data access |
| `pkt_generator/` | `shared/contracts/**/*.cs` -> `client/.../Generated/Contracts/*.cs` (diff-only, preserves `.meta`) |
| `stage_editor/` | Next.js stage editor — CSV CRUD + board UI + playtest |
| `all_generator.bat` | Runs all gen steps in order (`info_generator` -> `db_generator` -> `pkt_generator`) |
| `info_generator.bat` | Runs info_generator only |
| `db_generator.bat` | Runs db_generator only |
| `pkt_generator.bat` | Runs pkt_generator only |
| `stage_editor.bat` | Starts stage_editor dev server (sets PROJECT_ROOT, runs npm run dev) |
| `subset_tool/` | CSV-driven TMP source font subsetting before Unity release builds | -> `subset_tool/AGENTS.md` |
| `subset_fonts.bat` | Runs `subset_tool/subset_fonts.js` manually with logs; not part of `all_generator` |

## Rules
- **NEVER modify generated files manually** — always update the source and re-run the relevant generator
- ALL scripts read config from `config-loader.js` — never hardcode paths
- Tools default to `.env.dev`; use `CONFIG_ENV=prod` or `ENV_FILE=.env.prod` for production values
- `_` prefix files/dirs are skipped by all gen tools
- Errors report as: `[tool] ERROR: <file>\n  <location>: <message>`
- On any error — print all errors, then `process.exit(1)`
- db_generator dry-run mode is required in `template.ini`; set `false` to execute
- Each generator script uses `require('../config-loader')` (one level up from its subdir)
- `stage_editor.bat` sets `PROJECT_ROOT` env var so Next.js API routes resolve CSV paths correctly

## Adding a new gen tool
1. Create `tools/[name]/[name].js` — use `require('../config-loader')` for all config
2. Add `"gen:[name]": "node tools/[name]/[name].js"` to root `package.json` scripts
3. Add step to `all_generator.bat`
4. Update this Nav section
