# shared/datas/string — Localization String Tables

## Files
| file | PK | scope | role |
|------|----|-------|------|
| `client_string.csv` | `stringId` | C | UI text labels, button labels, popup titles |
| `error_messages.csv` | `errorCode` | C | Client-facing server error code translations |

## Key Naming Convention
`<screen>.<component>[.<variant>]`

| prefix | used for |
|--------|---------|
| `app.*` | App-level constants (title, tagline) |
| `boot.*` | Boot scene |
| `common.*` | Shared across screens (buttons, labels) |
| `error.*` | Generic client-side error messages |
| `nav.*` | Bottom navigation bar |
| `popup.<name>.*` | Popup/overlay specific strings |

Examples:
```
common.btn_retry
popup.pause.title
popup.pause.btn_resume
popup.settings.bgm
error.network_check
```

## Language Columns
Defined by `tools/subset_tool/config.json`. Current: `EN KO ZH_CN ZH_TW JA RU ES PT FR DE TH AR IT TR ID`

**MVP**: EN and KO filled. All other language columns are intentionally blank.
`LocalizationService` falls back to EN when the current language column is empty.

## Rules
- Keys use dot-namespace: `<screen>.<component>[.<variant>]`
- No numeric keys, no ALL_CAPS keys (use descriptive names)
- `_fmt` suffix on keys containing `{0}` format parameters
- No embedded newlines in CSV cell values (CsvLoader splits on `\n`)
- MVP blank columns: do NOT add NN constraint to non-EN/KO columns
- `error_messages.csv` errorCode must match server-returned error codes exactly

## Cross-refs
- Gen output: `client/project-flood/Assets/Resources/Data/string/`
- Consumed by: `Game.Services.LocalizationService`
- Depends on: `tools/subset_tool/config.json` (language list)
