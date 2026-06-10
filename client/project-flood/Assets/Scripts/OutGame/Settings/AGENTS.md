# OutGame/Settings — Settings Panel & Account Popup

Namespace: `Game.OutGame.Settings`

## Files
| file | class | role |
|------|-------|------|
| `SettingsPanelView.cs` | `SettingsPanelView` | Bottom-sheet popup: BGM/SFX/ScreenShake/Haptic toggles, language dropdown, version text |
| `AccountPopupView.cs` | `AccountPopupView` | Avatar/userID, Link Account (guest) or Switch/Logout (auth) |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `SettingsPanelView._bgmToggle` | SerializeField | Toggle; persisted to PlayerPrefs `setting_bgm` |
| `SettingsPanelView._sfxToggle` | SerializeField | Toggle; persisted to PlayerPrefs `setting_sfx` |
| `SettingsPanelView._screenShakeToggle` | SerializeField | Toggle; persisted to PlayerPrefs `setting_screen_shake` |
| `SettingsPanelView._hapticToggle` | SerializeField | Toggle; persisted to PlayerPrefs `setting_haptic_enabled` |
| `SettingsPanelView._langDropdown` | SerializeField | TMP_Dropdown; options built from `_supportedLangs`; saves via `LocalizationService.SetLanguage` |
| `SettingsPanelView._supportedLangs` | static field | `Language[]` — currently `{KO, EN}`; expand to add more languages |
| `SettingsPanelView._langStringIds`   | static field | CSV keys aligned with `_supportedLangs`; `LocalizationService.Get()` resolves to current-language name (e.g. EN→"Korean"/"English", KO→"한국어"/"영어") |
| `AccountPopupView` | component | Shows Link or Switch based on AuthService.IsGuest |

## Rules
- SettingsPanelView entry points: Lobby Header ⚙ button AND InGame Pause popup [Settings]
- AccountPopupView is NOT opened from SettingsPanelView; it is a standalone popup
- Both panels live on Canvas_Popup (via UIManager.ShowPopup)
- Language dropdown options use `_langStringIds` CSV keys → always renders in the active font atlas (no hardcoded strings); add new entry to both `_supportedLangs` and `_langStringIds` in tandem, and add the key to `client_string.csv`
- Google Sign-In: `OnLinkAccount` / `DoSwitchAccount` use `GoogleSignInBridge.SignIn` → `AuthService.LoginGoogle`; Android-only; guarded by `#if UNITY_ANDROID`
- `AppConfig.GoogleWebClientId` must be set before Google Sign-In works; shows error toast if empty

## Cross-refs
- Depends on: `Game.Core.UIManager`, `Game.Services.AuthService`, `Game.Services.LocalizationService`
