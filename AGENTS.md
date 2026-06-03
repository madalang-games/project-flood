# project-flood

## Nav
| path | role |
|------|------|
| `shared/` | Shared C# contracts, shared types, game meta data | → `shared/AGENTS.md` |
| `tools/` | Automation pipeline (gen-data, gen-packets, gen-orm) | → `tools/AGENTS.md` |
| `client/` | Unity 6 game client | → `client/AGENTS.md` |
| `server/` | ASP.NET Core 8 server + DB schema | → `server/AGENTS.md` |
| `docs/` | Design, technical, decisions, tests, platform refs | → `docs/AGENTS.md` |
| `TODO-List/` | Release tracker, per-area task lists | → `TODO-List/AGENTS.md` |
| `docker-compose.dev.bat` | Starts local dev Docker Compose stack | |

## Pipeline
```
shared/datas/**/*.csv  -> info_generator -> {client,server}/generated/data/**/*.csv
server/db/schema.json  -> db_generator   -> DB CREATE/ALTER TABLE (+ migration SQL)
shared/contracts/*.cs  -> pkt_generator  -> client/Assets/Scripts/Generated/Contracts/
```
CMD: `tools/gen-all.bat` | `tools/info_generator.bat` | `tools/db_generator.bat` | `npm run gen:all`

## Rules
- **AGENTS.md is the Source of Truth (SoT) for AI context.** `CLAUDE.md` and `GEMINI.md` must point to it via `@AGENTS.md`.
- **NEVER edit `*/generated/*`** — edit source (CSV, schema, contracts), re-run the appropriate generator.
- NEVER commit `.env` — use `.env.example`
- NEVER store secrets in `template.ini` — secrets go in `.env`
- CONFIG policy: env vars own deploy/runtime values; `template.ini` owns tooling values; no hardcoded config fallbacks
- `_` prefix files/dirs are skipped by all gen tools (examples, drafts)

## Clarification Protocol
Stop and ask **before** implementing when: requirement is ambiguous with design impact, a clearly better alternative exists (not just style), or task touches DB schema / auth / cross-service contracts.
Format: `QUESTION: [what] | OPTIONS: A) … B) … | RECOMMEND: [A/B] — [reason]`
Don't ask: clear best practice, cosmetic difference, same outcome different syntax.
Small improvement spotted → implement as requested + append `NOTE: [alternative] — ask to switch`.

## Documentation Convention
Every directory containing client/server/design/data/packet content must be documented.

**AGENTS.md** — AI-agent instructions, written in English, token-efficient:
- Leaf dirs: `## Files` table (file→class→role) + `## Symbols` table (symbol→kind→note) + `## Rules`
- Parent/nav dirs: `## Nav` table (path→role→link) + minimal `## Rules`
- Symbols use `ClassName.MemberName` notation — directly grep/searchable
- When new files are added → update that directory's `## Files` and `## Symbols`
- When a new subdirectory is created → create its AGENTS.md + update parent's `## Nav`
- When existing logic changes → update the affected symbol entries in AGENTS.md

**CLAUDE.md** — Contents must be exactly: `@AGENTS.md`
**GEMINI.md** — Contents must be exactly: `@AGENTS.md`

**Cross-refs** — add `## Cross-refs` to leaf and source-of-truth AGENTS.md:
- `Consumed by:` — classes/files that use this module's output
- `Depends on:` — classes/files this module reads/imports
- `Gen output:` — generated artifacts (data source files only)
- Use `Layer.ClassName` notation

## New System Checklist
When adding a cross-cutting system (touches ≥2 of: data / server / client):
1. `shared/datas/[domain]/` — define CSV schema → update AGENTS.md
2. `shared/contracts/` — define request/response DTOs → update contracts AGENTS.md
3. `server/db/schema.json` — add table definition → run `gen:orm`
4. Server layers (Domain → Infrastructure → API) — implement → update each AGENTS.md
5. Client — implement → update AGENTS.md
6. Run `tools/gen-all.bat`
7. Update `TODO-List/AGENTS.md` progress

## Search
Check already-loaded AGENTS.md context first. Use `rg` only when absent or stale.

| goal | first check | fallback |
|------|-------------|---------|
| file location / symbol | loaded `## Files` / `## Symbols` | `rg "ClassName" --type cs -l` |
| all implementors | loaded context | `rg "IInterface" --type cs -l` |
| role / ownership | loaded `## Nav` / `## Rules` | read that dir's `AGENTS.md` |

**Glob rules (token efficiency):**
- Always limit glob to specific path + extension: `client/project-flood/Assets/**/*.cs` — never `client/**/*` (pulls Unity Library/PackageCache noise)
- For directory structure exploration: delegate to `Explore` subagent — result is compressed before returning to main context (~60% token reduction)

## Output
- No narration before tool calls — execute immediately
- Silent on success path — only surface errors or blockers
- Final report: compact table or key-value pairs, no prose
