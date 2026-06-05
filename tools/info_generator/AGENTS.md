# tools/info_generator

## Files
| file | class | role |
|------|-------|------|
| `info_generator.js` | script | Generates client/server CSV outputs, C# data loaders, static-data POCOs, and data hashes |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `generateDomainPocoContent` | function | Uses `[data-gen].domain_static_data_namespace` |
| `generateInterfaceFile` | function | Uses `[data-gen].domain_interface_namespace` |
| `generateServiceFile` | function | Uses `[data-gen].infrastructure_data_namespace` and `server_namespace` |

## Rules
- Read all output paths and namespaces from `tools/config-loader.js` / `template.ini`.
- Do not hardcode project names such as `ProjectLink`.
- Generated `*.g.cs` files are written by this tool only; do not edit them manually.

## Cross-refs
- Depends on: `tools/config-loader.js`
- Consumed by: `tools/info_generator.bat`, `tools/all_generator.bat`
