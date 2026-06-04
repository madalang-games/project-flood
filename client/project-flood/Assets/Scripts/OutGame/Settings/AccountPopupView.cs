using Game.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.OutGame.Settings
{
    public class AccountPopupView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _userIdText;
        [SerializeField] private Button   _linkAccountButton;
        [SerializeField] private Button   _switchAccountButton;
        [SerializeField] private Button   _logoutButton;
        [SerializeField] private Button   _closeButton;

        private void Awake()
        {
            var auth = AuthService.Instance;
            bool isGuest = auth == null || auth.IsGuest;

            if (_userIdText != null)
                _userIdText.text = isGuest ? "Guest" : auth.UserId;

            if (_linkAccountButton   != null) _linkAccountButton.gameObject.SetActive(isGuest);
            if (_switchAccountButton != null) _switchAccountButton.gameObject.SetActive(!isGuest);

            _linkAccountButton?.onClick.AddListener(OnLinkAccount);
            _switchAccountButton?.onClick.AddListener(OnSwitchAccount);
            _logoutButton?.onClick.AddListener(OnLogout);
            if (_closeButton != null) _closeButton.onClick.AddListener(Close);
        }

        private void OnLinkAccount()
        {
            // Phase 2: OAuth flow
            Debug.Log("[AccountPopup] Link account — Phase 2");
        }

        private void OnSwitchAccount()
        {
            Game.Core.UIManager.Instance?.ShowPopup<Core.UI.ConfirmDialogView>(v => v.Init(
                title:        "Switch Account",
                body:         "Switching accounts will replace local data with the new account's data. Your current account data is preserved on the server.",
                confirmLabel: "Switch",
                onConfirm:    DoSwitchAccount,
                danger:       false));
        }

        private static void DoSwitchAccount()
        {
            // Phase 2: OAuth switch flow
            Debug.Log("[AccountPopup] Switch account — Phase 2");
        }

        private void OnLogout()
        {
            AuthService.Instance?.Logout();
            Close();
        }

        private void Close()
        {
            var appear = GetComponent<Core.UI.UIPanelAppear>();
            if (appear != null)
                appear.Disappear(() => Game.Core.UIManager.Instance?.CloseTopPopup());
            else
                Game.Core.UIManager.Instance?.CloseTopPopup();
        }
    }
}
