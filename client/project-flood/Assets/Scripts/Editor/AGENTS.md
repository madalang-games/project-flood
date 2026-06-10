# Editor — Unity Editor automation tools

## Files
| file | class | role |
|------|-------|------|
| `UIImageResourceExtractor.cs` | `UIImageResourceExtractor` | [MenuItem] batch-loads transparent UI images, parses alpha-connected components, previews and saves numbered PNG sprites |
| `BuildScript.cs` | `BuildScript` | Batch-mode Android build entry points; reads KEYSTORE_PASS/KEY_ALIAS_PASS from env; `BuildAndroidRelease` → ARM64 AAB |
| `ClearPlayerPrefs.cs` | `PlayerPrefsResetMenu` | [MenuItem] clears all PlayerPrefs for local reset/debug |
| `IconAutomator.cs` | `IconAutomator` | [MenuItem] icon DPI generator — resizes source icon and applies to all Android/iOS DPI slots |
| `GoogleMobileAdsGradleManifestPostprocessor.cs` | `GoogleMobileAdsGradleManifestPostprocessor` | Android Gradle postprocessor for GMA plugin conflicts |
| `UIEditorSetup.cs` | `UIEditorSetup` | [MenuItem] one-shot prefab/scene builders; attaches LocalizedText with stringId from StringIds.cs |
| `StageNodeEditorSetup.cs` | `StageNodeEditorSetup` | [MenuItem] StageNodeView prefab builder |
| `FontLocalizationConfigGenerator.cs` | `FontLocalizationConfigGenerator` | [MenuItem] reads tools/subset_tool/config.json → creates FontLocalizationConfig.asset with per-language fonts; sets TMP fallback |
| `StringIds.cs` | `StringIds` | **AUTO-GENERATED** by `gen:info` from `client_string.csv`; key constants; used by UIEditorSetup via `using static` |
| `StringCsvPostprocessor.cs` | `StringCsvPostprocessor` | AssetPostprocessor; watches `Data/string/client_string.csv` reimport → calls `LocalizedText.RefreshAllInEditor()`; menu: `Tools/Localization/Refresh Editor Text Preview` |
| `UnityStageFileWatcher.cs` | `UnityStageFileWatcher` | [InitializeOnLoad] watches changes to shared `stage.csv` and auto-runs generation scripts |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `UIImageResourceExtractor.Open()` | method | [MenuItem "Tools/UI/Image Resource Extractor"] opens the extraction window |
| `BuildScript.BuildAndroidRelease()` | method | Batch entry: ARM64 AAB for Play Store; reads KEYSTORE_PASS/KEY_ALIAS_PASS env vars |
| `PlayerPrefsResetMenu.ResetPrefs()` | method | [MenuItem "Tools/Reset PlayerPrefs"] deletes all PlayerPrefs |
| `IconAutomator.ShowWindow()` | method | [MenuItem "Tools/Icon Automator"] opens icon automation window |
| `UIEditorSetup.CreateAllPrefabs()` | method | [MenuItem "Tools/UI Setup/1"] creates all popup/overlay prefabs |
| `UIEditorSetup.TryMapImageSprite()` | method | Helper to map sprite directly onto Image component target |
| `UIEditorSetup.BtnNavTab()` | method | Nav bar tab button — icon (color-tinted) above label; `_homeHighlight` etc. wire to `Visual/Icon` Image |
| `UIEditorSetup.ItemToggleRow()` | method | Toggle row with item icon on left; Label TMP is a child of Toggle GO (GetComponentInChildren finds it) |
| `UIEditorSetup.MapStarAndIconSprites()` | method | Re-maps star_empty/star_filled/lock/ExtraTurns icon on Common prefabs without recreating them |
| `UIEditorSetup.MapHierarchyImageSprite()` | method | Maps a sprite key from resMap to an Image at `childPath` inside a loaded prefab |
| `FontLocalizationConfigGenerator.Generate()` | method | [MenuItem "Tools/Localization/Generate Font Config"] creates FontLocalizationConfig.asset from config.json |
| `StringIds` | class | All client_string.csv key constants; import with `using static Game.Editor.StringIds` |
| `UnityStageFileWatcher` | class | Active file system watcher for local hot-reloads |

## Rules
- Editor-only folder — auto-excluded from player builds
- DO NOT add game logic or runtime dependencies here
