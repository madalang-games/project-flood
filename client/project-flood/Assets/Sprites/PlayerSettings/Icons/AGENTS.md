# PlayerSettings Icons

## Files
| file | class | role |
|------|-------|------|
| `AppIcon.png` | asset | Existing app icon source sprite |
| `AppIcon_FloodFill.png` | asset | Pixel-art flood-fill board app icon source sprite |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `AppIcon` | sprite asset | Current existing app icon source |
| `AppIcon_FloodFill` | sprite asset | 1024x1024 flood-fill board icon variant |

## Cross-refs
| type | refs |
|------|------|
| Consumed by | `Unity.PlayerSettings` app icon configuration |

## Rules
- Keep icon sources square PNGs unless a platform-specific export requires another format.
- Preserve matching `.meta` files so Unity import settings and GUIDs remain stable.
