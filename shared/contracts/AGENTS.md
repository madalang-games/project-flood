# shared/contracts - C# DTO Contracts

## Overview
Target: `netstandard2.1` (Unity-compatible).
Namespace root: `ProjectFlood.Contracts.[Domain]`.
File format: `[Domain]Requests.cs`, `[Domain]Responses.cs`.

Consumed by:
- Server: via `<ProjectReference>` in `ProjectFlood.API.csproj` and `ProjectFlood.Application.csproj`
- Client: auto-synced via `pkt_generator` -> `Assets/Scripts/Generated/Contracts/`

## Nav
| path | role |
|------|------|
| `Common/` | Shared error/result types |
| `GameTypes/` | Shared game-type enums (CellType, Difficulty) |
| `Stamina/` | Stamina status and ad life reward DTOs |
| `Stage/` | Stage attempt and revive DTOs |
| `Rewards/` | Generic reward source and ad reward DTOs |
| `Currency/` | Soft currency snapshot DTO |
| `Ad/` | Ad eligibility, interstitial, double reward DTOs |
| `Ranking/` | Global and stage ranking DTOs |

## Rules
- `netstandard2.1` only; no C# 10+ features, no nullable reference types at project level (use `#nullable enable` per file).
- No business logic in DTOs; plain properties only.
- File naming: `[Domain]Requests.cs` and `[Domain]Responses.cs` per domain.
- When adding a domain: create the subdirectory + both files + update Nav above.
- NEW_DIR: create `AGENTS.md` for it + update Nav above.

## Cross-refs
- Gen output: `client/project-flood/Assets/Scripts/Generated/Contracts/` (via `pkt_generator`)
- Consumed by: `ProjectFlood.API`, `ProjectFlood.Application`
