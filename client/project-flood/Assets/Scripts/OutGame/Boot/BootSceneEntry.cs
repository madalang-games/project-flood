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
            Debug.Log("[Boot] Start");

            if (UIManager.Instance == null)
                Debug.LogError("[Boot] UIManager.Instance is null — add UIManager GameObject to Boot scene");

            if (SceneTransition.Instance == null)
                Debug.LogWarning("[Boot] SceneTransition.Instance is null — add SceneTransition GameObject to Boot scene");

            if (AuthService.Instance == null)
            {
                Debug.LogError("[Boot] AuthService.Instance is null — add AuthService GameObject to Boot scene");
                return;
            }

            UIManager.Instance?.ShowLoading();
            AuthService.Instance.Initialize(OnAuthResult);
        }

        private void OnAuthResult(AuthResult result)
        {
            Debug.Log($"[Boot] AuthResult = {result}");
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
            Debug.Log($"[Boot] GoToLobby → scene='{LobbyScene}'");

#if UNITY_EDITOR
            // Validate scene is in Build Settings
            bool inBuild = false;
            foreach (var s in UnityEditor.EditorBuildSettings.scenes)
                if (s.path.EndsWith($"/{LobbyScene}.unity")) { inBuild = true; break; }
            if (!inBuild)
                Debug.LogError($"[Boot] '{LobbyScene}' not found in Build Settings → File > Build Settings > Add Open Scenes");
#endif

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
