using System;
using UnityEngine;

namespace Game.Services
{
    public enum AuthResult { Authenticated, Guest, ReLoginRequired }

    public class AuthService : MonoBehaviour
    {
        public static AuthService Instance { get; private set; }

        public bool   IsGuest { get; private set; } = true;
        public string UserId  { get; private set; } = string.Empty;

        private const string KeyUserId       = "auth_user_id";
        private const string KeyIsOAuthLinked = "auth_oauth_linked";

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void Initialize(Action<AuthResult> onComplete)
        {
            // Access token check → silent refresh → device UUID fallback
            // Full server auth is Phase 2. Stub: always guest on first launch.
            string cachedId      = PlayerPrefs.GetString(KeyUserId, null);
            bool   oauthLinked   = PlayerPrefs.GetInt(KeyIsOAuthLinked, 0) == 1;

            if (!string.IsNullOrEmpty(cachedId) && oauthLinked)
            {
                // In a real implementation this would attempt token refresh.
                // Stub: simulate re-login required when OAuth account exists but no live session.
                onComplete?.Invoke(AuthResult.ReLoginRequired);
                return;
            }

            // New or reinstalled user — create/reuse device UUID guest session
            if (string.IsNullOrEmpty(cachedId))
                cachedId = System.Guid.NewGuid().ToString("N");

            PlayerPrefs.SetString(KeyUserId, cachedId);
            UserId  = cachedId;
            IsGuest = true;
            onComplete?.Invoke(AuthResult.Guest);
        }

        public void LinkOAuth(string oauthUserId)
        {
            UserId  = oauthUserId;
            IsGuest = false;
            PlayerPrefs.SetString(KeyUserId, oauthUserId);
            PlayerPrefs.SetInt(KeyIsOAuthLinked, 1);
        }

        public void Logout()
        {
            PlayerPrefs.DeleteKey(KeyUserId);
            PlayerPrefs.DeleteKey(KeyIsOAuthLinked);
            IsGuest = true;
            UserId  = string.Empty;
        }
    }
}
