# stage_editor/src/workers

## Files
| file | class/export | role |
|------|--------------|------|
| `generator.worker.ts` | `GenerateWorkerRequest`, `GenerateWorkerResponse` | Web Worker entrypoint for one stage generator attempt per message |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `workerScope.onmessage` | handler | Runs `generateBoardAttempt(config, attempt)` and posts result/error |

## Rules
- Worker messages must stay structured-clone safe.
- Keep each worker task to one generator attempt; scheduling belongs in `src/lib/generator-worker-pool.ts`.

## Cross-refs
- Consumed by: `StageEditor.GeneratorWorkerPool`
- Depends on: `StageEditor.Generator`
