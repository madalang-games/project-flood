# Editor — Unity Editor automation tools

## Files
| file | class | role |
|------|-------|------|
| `UIImageResourceExtractor.cs` | `UIImageResourceExtractor` | [MenuItem] batch-loads transparent UI images, parses alpha-connected components, previews and saves numbered PNG sprites |
| `BuildScript.cs` | `BuildScript` | Batch-mode Android build entry points; reads KEYSTORE_PASS/KEY_ALIAS_PASS from env; `BuildAndroidRelease` → ARM64 AAB |
| `ClearPlayerPrefs.cs` | `PlayerPrefsResetMenu` | [MenuItem] clears all PlayerPrefs for local reset/debug |
| `IconAutomator.cs` | `IconAutomator` | [MenuItem] icon DPI generator — resizes source icon and applies to all Android/iOS DPI slots |
| `GoogleMobileAdsGradleManifestPostprocessor.cs` | `GoogleMobileAdsGradleManifestPostprocessor` | Android Gradle postprocessor for GMA plugin conflicts |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `UIImageResourceExtractor.Open()` | method | [MenuItem "Tools/UI/Image Resource Extractor"] opens the extraction window |
| `UIImageResourceExtractor.ParseAllSources()` | method | parses all added images into alpha-connected resource components |
| `UIImageResourceExtractor.SaveResources()` | method | saves parsed previews as `baseFileName_N.png` sprites |
| `BuildScript.BuildAndroidRelease()` | method | Batch entry: ARM64 AAB for Play Store; reads KEYSTORE_PASS/KEY_ALIAS_PASS env vars |
| `PlayerPrefsResetMenu.ResetPrefs()` | method | [MenuItem "Tools/Reset PlayerPrefs"] deletes all PlayerPrefs |
| `IconAutomator.ShowWindow()` | method | [MenuItem "Tools/Icon Automator"] opens icon automation window |
| `IconAutomator.GenerateAndApply()` | method | resizes source icon and applies to PlayerSettings for all DPI sizes |

## Rules
- Editor-only folder — auto-excluded from player builds
- DO NOT add game logic or runtime dependencies here
