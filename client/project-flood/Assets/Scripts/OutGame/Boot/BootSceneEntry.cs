using Game.Core;
using Game.Services;
using UnityEngine;

namespace Game.OutGame.Boot
{
    public class BootSceneEntry : MonoBehaviour
    {
        [SerializeField] private BootView _bootView;

        private const string LobbyScene = "Lobby";

        private void Start()
        {
            UIManager.Instance?.ShowLoading();
            AuthService.Instance.Initialize(OnAuthResult);
        }

        private void OnAuthResult(AuthResult result)
        {
            UIManager.Instance?.HideLoading();

            switch (result)
            {
                case AuthResult.Authenticated:
                case AuthResult.Guest:
                    GoToLobby();
                    break;

                case AuthResult.ReLoginRequired:
                    ShowReLoginScreen();
                    break;
            }
        }

        private void GoToLobby()
        {
            if (SceneTransition.Instance != null)
                SceneTransition.Instance.FadeToScene(LobbyScene);
            else
                UnityEngine.SceneManagement.SceneManager.LoadScene(LobbyScene);
        }

        private void ShowReLoginScreen()
        {
            UIManager.Instance?.ShowPopup<ReLoginView>(v => v.Init(
                onReLogin:         () => AuthService.Instance.Initialize(OnAuthResult),
                onContinueAsGuest: OnContinueAsGuest));
        }

        private void OnContinueAsGuest()
        {
            UIManager.Instance?.ShowPopup<Core.UI.ConfirmDialogView>(v => v.Init(
                title:        "Continue as Guest?",
                body:         "Progress linked to your account will not be accessible.",
                confirmLabel: "Continue",
                onConfirm:    GoToLobby,
                cancelLabel:  "Cancel",
                danger:       false));
        }
    }
}
