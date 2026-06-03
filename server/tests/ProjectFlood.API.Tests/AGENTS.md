# ProjectFlood.API.Tests

## Files
| file | class | role |
|------|-------|------|
| `ProjectFlood.API.Tests.csproj` | project | xUnit API integration test project |

## Rules
- Keep tests deterministic and engine-free
- Do not connect to real MySQL, Redis, or platform auth from this project
- In-memory EF database name shared across factory lifetime; seed data inserted once in CreateHost
- Per-test isolation: use Guid-named in-memory DB instances
- NEW_DIR: create `AGENTS.md` for it + update Nav above
