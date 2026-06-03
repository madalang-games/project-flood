# client - Unity 6 (URP 2D)

## Stack
Unity 6 | URP 2D | C# | New Input System

## Nav
| path | role |
|------|------|
| `project-flood/Assets/Scripts/Core/` | App lifecycle, singletons, FSM, managers |
| `project-flood/Assets/Scripts/InGame/` | Game-specific gameplay domain (flood mechanics) |
| `project-flood/Assets/Scripts/OutGame/` | Non-gameplay scenes (Title, Lobby) |
| `project-flood/Assets/Scripts/Services/` | Client service boundaries for server/static data |
| `project-flood/Assets/Scripts/Data/Generated/` | Auto-generated C# models — DO NOT EDIT |
| `project-flood/Assets/Scripts/Generated/Contracts/` | Auto-synced from `shared/contracts/` via pkt_generator |
| `project-flood/Assets/Scripts/Utils/` | Stateless helpers |
| `project-flood/Assets/Scripts/Editor/` | Unity Editor-only automation tools |
| `project-flood/Assets/Prefabs/` | Runtime prefabs |
| `project-flood/Assets/Resources/Data/` | Runtime CSVs — generated, DO NOT EDIT |
| `project-flood/Assets/Resources/Prefabs/UI/` | Runtime-loaded popup prefabs |
| `project-flood/Assets/Plugins/` | Platform native plugins |
| `project-flood/Assets/Scenes/` | Unity scenes |

## Rules
- NEVER edit `Assets/Resources/Data/` — source is `shared/datas/`, regenerate with `npm run gen:info`
- NEVER edit `Assets/Scripts/Data/Generated/` or `Assets/Scripts/Generated/` — regenerate with gen tools
- NEW_DIR: create `AGENTS.md` for it + update Nav above

## Conventions
- Namespace mirrors folder path: `Game.InGame.Board`, `Game.OutGame.UI`, etc.
- MonoBehaviour suffix: `View` (e.g. `BoardView`, `NodeView`)
- Pure data/logic classes: no suffix (e.g. `Board`, `FloodModel`)
- Input: New Input System via `InputSystem_Actions.inputactions`

## Cross-refs
| type | refs |
|------|------|
| Depends on | `docs/refs/platform-auth.md` |
| Platform source | `platform:docs/refs/auth.md` |
