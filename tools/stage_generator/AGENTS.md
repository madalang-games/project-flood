# stage_generator - C# Local Stage Generator

Offline content-generation CLI for stage editor. Receives generator config as JSON, samples candidates in parallel, solves with exact BFS, scores board quality, and returns the best candidate JSON.

## Files
| file | class/export | role |
|------|--------------|------|
| `StageGenerator.Cli.csproj` | project | .NET 8 console app |
| `Program.cs` | `Program` | CLI entry: read request JSON path, run generator, write result JSON |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `GeneratorRequest` | record | Input contract from stage editor API |
| `GeneratorResult` | record | Output contract: board, solution, attempts, solveLength, score |
| `StageGenerator.Generate` | method | Runs attempts with `Parallel.For` and returns highest score |
| `Solver.AutoSolveExact` | method | BFS min-move solver with gravity, conveyor, portal, rotation support |
| `BoardRules.ApplyGravity` | method | C# port of stage editor gravity semantics |

## Rules
- CLI must write machine-readable JSON to stdout on success.
- Diagnostics/errors go to stderr; nonzero exit means API failure.
- Keep request/result contracts in sync with `tools/stage_editor/src/app/api/generate-board/route.ts`.
- **NEVER use `dotnet run`** — always invoke via `dotnet exec bin/publish/StageGenerator.Cli.dll`. `dotnet run` adds ~1.2s MSBuild overhead per call regardless of `--no-restore`.
- Published DLL path: `tools/stage_generator/bin/publish/StageGenerator.Cli.dll` (Release, framework-dependent). `bin/` is gitignored — `stage_editor.bat` auto-publishes on startup if DLL is missing or source is newer.

## Cross-refs
- Consumed by: `StageEditor.ApiGenerateBoard`
- Depends on: no project runtime services; local CLI only
- Gen output: none; result is imported by stage editor UI
