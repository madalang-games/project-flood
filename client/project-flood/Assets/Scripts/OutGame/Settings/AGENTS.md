# OutGame/Settings — Settings Panel & Account Popup

Namespace: `Game.OutGame.Settings`

## Files
| file | class | role |
|------|-------|------|
| `SettingsPanelView.cs` | `SettingsPanelView` | Bottom-sheet popup: BGM/SFX/ScreenShake toggles, Account button, version text |
| `AccountPopupView.cs` | `AccountPopupView` | Avatar/userID, Link Account (guest) or Switch/Logout (auth) |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `SettingsPanelView._bgmToggle` | SerializeField | Toggle; persisted to PlayerPrefs `setting_bgm` |
| `SettingsPanelView._sfxToggle` | SerializeField | Toggle; persisted to PlayerPrefs `setting_sfx` |
| `SettingsPanelView._screenShakeToggle` | SerializeField | Toggle; persisted to PlayerPrefs `setting_screen_shake` |
| `AccountPopupView` | component | Shows Link or Switch based on AuthService.IsGuest |

## Rules
- SettingsPanelView entry points: Lobby Header ⚙ button AND InGame Pause popup [Settings]
- Both panels live on Canvas_Popup (via UIManager.ShowPopup)
- Account switching (Phase 2): stub logs + no actual OAuth flow yet

## Cross-refs
- Depends on: `Game.Core.UIManager`, `Game.Services.AuthService`
