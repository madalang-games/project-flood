using System;
using System.Collections;
using System.Collections.Generic;
using Game.Core;
using UnityEngine;
using UnityEngine.Networking;

namespace Game.Services
{
    public enum AuthResult { Authenticated, Guest, ReLoginRequired }

    public class AuthService : MonoBehaviour
    {
        public static AuthService Instance { get; private set; }

        [SerializeField] private string _baseUrl = "http://localhost:5000";
        [SerializeField] private string _clientVersion = "1.0.0";
        [SerializeField] private string _protocolVersion = "1";

        public event Action<bool, string> OnAuthStateChanged; // (isAuthenticated, provider)

        private const string AccessTokenKey = "Auth.AccessToken";
        private const string RefreshTokenKey = "Auth.RefreshToken";
        private const string AccessExpiresAtKey = "Auth.AccessExpiresAt";
        private const string ProviderKey = "Auth.Provider";
        private const string ClientIdKey = "Auth.ClientId";

        private ITokenStorage _storage;
        private string _accessToken = string.Empty;
        private string _refreshToken = string.Empty;
        private DateTimeOffset _accessExpiresAt;
        private string _provider = string.Empty;
        private bool _refreshInFlight;
        private List<Action<bool, string>> _refreshWaiters;

        public bool IsAuthenticated => !string.IsNullOrEmpty(_accessToken);
        public bool IsGuest => _provider == "guest";
        public string UserId => _storage?.Get(ClientIdKey) ?? string.Empty;
        public string AccessToken => _accessToken;

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            _storage = new SecureTokenStorage();
            LoadSession();
        }

        public void Initialize(Action<AuthResult> onComplete)
        {
            if (HasUsableAccessToken())
            {
                SyncServiceTokens();
                onComplete?.Invoke(IsGuest ? AuthResult.Guest : AuthResult.Authenticated);
                return;
            }

            if (!string.IsNullOrEmpty(_refreshToken))
            {
                Refresh((success, error) =>
                {
                    if (success)
                    {
                        onComplete?.Invoke(IsGuest ? AuthResult.Guest : AuthResult.Authenticated);
                    }
                    else
                    {
                        onComplete?.Invoke(AuthResult.ReLoginRequired);
                    }
                });
                return;
            }

            LoginGuest((success, error) =>
            {
                if (success)
                {
                    onComplete?.Invoke(AuthResult.Guest);
                }
                else
                {
                    // Fallback to offline/mock if server is not running in editor
                    Debug.LogWarning($"[AUTH] Server guest login failed: {error}. Falling back to offline guest.");
                    SetupOfflineGuest();
                    onComplete?.Invoke(AuthResult.Guest);
                }
            });
        }

        public void LoginGuest(Action<bool, string> onComplete)
        {
            var clientId = GetOrCreateClientId();
            var req = new GuestLoginRequestJson { clientId = clientId, displayName = null };
            StartCoroutine(Post("/api/auth/guest", JsonUtility.ToJson(req), text =>
            {
                var response = JsonUtility.FromJson<AuthResponseJson>(text);
                CompleteSession(true, "", "guest", response, onComplete);
            }, error => onComplete?.Invoke(false, error)));
        }

        public void LoginGoogle(string idToken, string nonce, Action<bool, string> onComplete)
        {
            var clientId = GetOrCreateClientId();
            var req = new GoogleLoginRequestJson
            {
                clientId = clientId,
                idToken = idToken,
                nonce = string.IsNullOrEmpty(nonce) ? null : nonce,
                guestRefreshToken = string.IsNullOrEmpty(_refreshToken) ? null : _refreshToken
            };
            StartCoroutine(Post("/api/auth/google", JsonUtility.ToJson(req), text =>
            {
                var response = JsonUtility.FromJson<AuthResponseJson>(text);
                CompleteSession(true, "", "google", response, onComplete);
            }, error => onComplete?.Invoke(false, error)));
        }

        public void Refresh(Action<bool, string> onComplete)
        {
            if (string.IsNullOrEmpty(_refreshToken))
            {
                onComplete?.Invoke(false, "SESSION_EXPIRED");
                return;
            }

            if (_refreshInFlight)
            {
                if (_refreshWaiters == null) _refreshWaiters = new List<Action<bool, string>>();
                _refreshWaiters.Add(onComplete);
                return;
            }

            _refreshInFlight = true;
            var req = new RefreshRequestJson { refreshToken = _refreshToken };
            StartCoroutine(Post("/api/auth/refresh", JsonUtility.ToJson(req), text =>
            {
                var response = JsonUtility.FromJson<AuthResponseJson>(text);
                _refreshInFlight = false;
                var waiters = _refreshWaiters;
                _refreshWaiters = null;

                CompleteSession(true, "", _provider, response, (ok, err) =>
                {
                    onComplete?.Invoke(ok, err);
                    if (waiters != null)
                        foreach (var w in waiters) w?.Invoke(ok, err);
                });
            }, error =>
            {
                _refreshInFlight = false;
                var waiters = _refreshWaiters;
                _refreshWaiters = null;

                ClearToken();
                onComplete?.Invoke(false, error);
                if (waiters != null)
                    foreach (var w in waiters) w?.Invoke(false, error);
            }));
        }

        public void Logout(Action<bool, string> onComplete = null)
        {
            if (string.IsNullOrEmpty(_refreshToken))
            {
                ClearToken();
                onComplete?.Invoke(true, "");
                return;
            }

            var req = new LogoutRequestJson { refreshToken = _refreshToken, reason = "client_logout" };
            StartCoroutine(Post("/api/auth/logout", JsonUtility.ToJson(req), text =>
            {
                ClearToken();
                onComplete?.Invoke(true, "");
            }, error =>
            {
                ClearToken();
                onComplete?.Invoke(true, ""); // Force logout locally even if request fails
            }));
        }

        private void CompleteSession(bool ok, string error, string provider, AuthResponseJson auth, Action<bool, string> onComplete)
        {
            if (!ok || auth == null || string.IsNullOrEmpty(auth.accessToken))
            {
                onComplete?.Invoke(false, string.IsNullOrEmpty(error) ? "AUTH_FAILED" : error);
                return;
            }

            _accessToken = auth.accessToken;
            _refreshToken = auth.refreshToken;
            _provider = provider;
            
            if (DateTimeOffset.TryParse(auth.expiresAt, out var expires))
                _accessExpiresAt = expires;
            else
                _accessExpiresAt = DateTimeOffset.UtcNow.AddHours(1);

            _storage.Set(AccessTokenKey, _accessToken);
            _storage.Set(RefreshTokenKey, _refreshToken);
            _storage.Set(ProviderKey, _provider);
            _storage.Set(AccessExpiresAtKey, _accessExpiresAt.ToString("O"));
            _storage.Save();

            SyncServiceTokens();
            OnAuthStateChanged?.Invoke(true, _provider);
            onComplete?.Invoke(true, "");
        }

        private void SetupOfflineGuest()
        {
            _provider = "guest";
            _accessToken = "offline_access_token";
            _refreshToken = "offline_refresh_token";
            _accessExpiresAt = DateTimeOffset.UtcNow.AddDays(30);

            _storage.Set(AccessTokenKey, _accessToken);
            _storage.Set(RefreshTokenKey, _refreshToken);
            _storage.Set(ProviderKey, _provider);
            _storage.Set(AccessExpiresAtKey, _accessExpiresAt.ToString("O"));
            _storage.Save();

            SyncServiceTokens();
            OnAuthStateChanged?.Invoke(true, _provider);
        }

        private void ClearToken()
        {
            _accessToken = string.Empty;
            _refreshToken = string.Empty;
            _provider = string.Empty;
            _accessExpiresAt = default;

            _storage.Delete(AccessTokenKey);
            _storage.Delete(RefreshTokenKey);
            _storage.Delete(ProviderKey);
            _storage.Delete(AccessExpiresAtKey);
            _storage.Save();

            SyncServiceTokens();
            OnAuthStateChanged?.Invoke(false, string.Empty);
        }

        private void SyncServiceTokens()
        {
            StageApiService.Instance?.SetAuthToken(_accessToken);
            RankingApiService.Instance?.SetAuthToken(_accessToken);
            StaminaApiService.Instance?.SetAuthToken(_accessToken);
        }

        private void LoadSession()
        {
            _accessToken = _storage.Get(AccessTokenKey);
            _refreshToken = _storage.Get(RefreshTokenKey);
            _provider = _storage.Get(ProviderKey);
            
            var expiresStr = _storage.Get(AccessExpiresAtKey);
            if (!DateTimeOffset.TryParse(expiresStr, out _accessExpiresAt))
                _accessExpiresAt = default;
        }

        private bool HasUsableAccessToken()
        {
            return !string.IsNullOrEmpty(_accessToken) && _accessExpiresAt > DateTimeOffset.UtcNow.AddMinutes(5);
        }

        private string GetOrCreateClientId()
        {
            var id = _storage.Get(ClientIdKey);
            if (!string.IsNullOrEmpty(id)) return id;

            id = Guid.NewGuid().ToString("N");
            _storage.Set(ClientIdKey, id);
            _storage.Save();
            return id;
        }

        private IEnumerator Post(string path, string jsonPayload, Action<string> onSuccess, Action<string> onError)
        {
            var url = $"{_baseUrl.TrimEnd('/')}/{path.TrimStart('/')}";
            using var req = new UnityWebRequest(url, "POST");
            var bytes = System.Text.Encoding.UTF8.GetBytes(jsonPayload);
            req.uploadHandler = new UploadHandlerRaw(bytes);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.SetRequestHeader("X-Client-Version", _clientVersion);
            req.SetRequestHeader("X-Protocol-Version", _protocolVersion);

            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                onError?.Invoke(req.error);
                yield break;
            }

            onSuccess?.Invoke(req.downloadHandler.text);
        }

        // --- JSON Helper Classes for Unity JsonUtility ---
        [Serializable]
        private class GuestLoginRequestJson
        {
            public string clientId;
            public string displayName;
        }

        [Serializable]
        private class GoogleLoginRequestJson
        {
            public string clientId;
            public string idToken;
            public string nonce;
            public string guestRefreshToken;
        }

        [Serializable]
        private class RefreshRequestJson
        {
            public string refreshToken;
        }

        [Serializable]
        private class LogoutRequestJson
        {
            public string refreshToken;
            public string reason;
        }

        [Serializable]
        private class ProfileJson
        {
            public string userId;
            public string pid;
            public string displayName;
            public bool isGuest;
            public List<string> linkedProviders;
            public int avatarId;
            public string createdAt;
        }

        [Serializable]
        private class AuthResponseJson
        {
            public string accessToken;
            public string refreshToken;
            public string expiresAt;
            public ProfileJson profile;
        }
    }
}
