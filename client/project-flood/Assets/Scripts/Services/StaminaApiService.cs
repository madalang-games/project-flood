using System;
using System.Collections;
using System.Globalization;
using ProjectFlood.Contracts.Stamina;
using UnityEngine;
using UnityEngine.Networking;

#pragma warning disable 0649
namespace Game.Services
{
    public class StaminaApiService : MonoBehaviour
    {
        public static StaminaApiService Instance { get; private set; }

        [SerializeField] private string _baseUrl = "http://localhost:5000";
        private string _authToken = string.Empty;

        public event Action OnStaminaUpdated;
        
        private StaminaSnapshot _latestStamina;
        private DateTimeOffset _serverTimeAtLatestUpdate;
        private float _localTimeAtLatestUpdate;

        public StaminaSnapshot LatestStamina => _latestStamina;

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void SetAuthToken(string authToken) => _authToken = authToken;

        public void UpdateStamina(StaminaSnapshot snapshot, DateTimeOffset serverTime)
        {
            _latestStamina = snapshot;
            _serverTimeAtLatestUpdate = serverTime;
            _localTimeAtLatestUpdate = Time.realtimeSinceStartup;
            OnStaminaUpdated?.Invoke();
        }

        public void FetchStamina(Action<StaminaStatusResponse> onSuccess = null, Action<string> onError = null)
        {
            StartCoroutine(Get($"{_baseUrl}/api/stamina", text =>
            {
                var json = JsonUtility.FromJson<StaminaStatusJson>(text);
                var response = json.ToContract();
                UpdateStamina(response.Stamina, response.ServerTime);
                onSuccess?.Invoke(response);
            }, onError));
        }

        public void ClaimAdLife(string provider, string adToken, Action<StaminaAdLifeRewardResponse> onSuccess = null, Action<string> onError = null)
        {
            var body = $"{{\"provider\":\"{Escape(provider)}\",\"adToken\":\"{Escape(adToken)}\"}}";
            StartCoroutine(Post($"{_baseUrl}/api/stamina/ad-life-reward", body, text =>
            {
                var json = JsonUtility.FromJson<StaminaAdLifeRewardJson>(text);
                var response = json.ToContract();
                UpdateStamina(response.Stamina, response.ServerTime);
                onSuccess?.Invoke(response);
            }, onError));
        }

        public int GetEstimatedLife()
        {
            if (_latestStamina == null) return 5;
            if (_latestStamina.IsUnlimited) return _latestStamina.Current; // Unlimited ignores cap

            if (_latestStamina.Current >= _latestStamina.Max)
                return _latestStamina.Current;

            if (!_latestStamina.NextRechargeAt.HasValue)
                return _latestStamina.Current;

            var elapsed = Time.realtimeSinceStartup - _localTimeAtLatestUpdate;
            var currentServerTime = _serverTimeAtLatestUpdate.AddSeconds(elapsed);
            var rechargeTime = _latestStamina.NextRechargeAt.Value;

            if (currentServerTime >= rechargeTime)
            {
                var secondsSinceRecharge = (currentServerTime - rechargeTime).TotalSeconds;
                var extraLives = 1 + (int)(secondsSinceRecharge / _latestStamina.RegenSeconds);
                return Math.Min(_latestStamina.Max, _latestStamina.Current + extraLives);
            }

            return _latestStamina.Current;
        }

        public double GetSecondsToNextRecharge()
        {
            if (_latestStamina == null || _latestStamina.Current >= _latestStamina.Max || !_latestStamina.NextRechargeAt.HasValue)
                return 0;

            var elapsed = Time.realtimeSinceStartup - _localTimeAtLatestUpdate;
            var currentServerTime = _serverTimeAtLatestUpdate.AddSeconds(elapsed);
            var rechargeTime = _latestStamina.NextRechargeAt.Value;

            if (currentServerTime >= rechargeTime)
            {
                var secondsSinceRecharge = (currentServerTime - rechargeTime).TotalSeconds;
                var remainingInPeriod = _latestStamina.RegenSeconds - (secondsSinceRecharge % _latestStamina.RegenSeconds);
                return remainingInPeriod;
            }

            return (rechargeTime - currentServerTime).TotalSeconds;
        }

        public double GetSecondsOfUnlimitedRemaining()
        {
            if (_latestStamina == null || !_latestStamina.IsUnlimited || !_latestStamina.UnlimitedUntil.HasValue)
                return 0;

            var elapsed = Time.realtimeSinceStartup - _localTimeAtLatestUpdate;
            var currentServerTime = _serverTimeAtLatestUpdate.AddSeconds(elapsed);
            var unlimitedTime = _latestStamina.UnlimitedUntil.Value;

            if (currentServerTime >= unlimitedTime)
                return 0;

            return (unlimitedTime - currentServerTime).TotalSeconds;
        }

        private IEnumerator Get(string url, Action<string> onSuccess, Action<string> onError)
        {
            using var req = UnityWebRequest.Get(url);
            req.SetRequestHeader("Content-Type", "application/json");
            if (!string.IsNullOrEmpty(_authToken))
                req.SetRequestHeader("Authorization", $"Bearer {_authToken}");

            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                onError?.Invoke(req.error);
                yield break;
            }

            onSuccess?.Invoke(req.downloadHandler.text);
        }

        private IEnumerator Post(string url, string body, Action<string> onSuccess, Action<string> onError)
        {
            using var req = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST);
            var bytes = System.Text.Encoding.UTF8.GetBytes(body);
            req.uploadHandler = new UploadHandlerRaw(bytes);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            if (!string.IsNullOrEmpty(_authToken))
                req.SetRequestHeader("Authorization", $"Bearer {_authToken}");

            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                onError?.Invoke(req.error);
                yield break;
            }

            onSuccess?.Invoke(req.downloadHandler.text);
        }

        private static string Escape(string value) => (value ?? string.Empty).Replace("\\", "\\\\").Replace("\"", "\\\"");

        [Serializable]
        private class StaminaSnapshotJson
        {
            public int current;
            public int max;
            public string nextRechargeAt;
            public string unlimitedUntil;
            public bool isUnlimited;
            public int regenSeconds;

            public StaminaSnapshot ToContract() => new StaminaSnapshot
            {
                Current = current,
                Max = max,
                NextRechargeAt = ParseTime(nextRechargeAt),
                UnlimitedUntil = ParseTime(unlimitedUntil),
                IsUnlimited = isUnlimited,
                RegenSeconds = regenSeconds,
            };
        }

        [Serializable]
        private class StaminaStatusJson
        {
            public StaminaSnapshotJson stamina;
            public string serverTime;

            public StaminaStatusResponse ToContract() => new StaminaStatusResponse
            {
                Stamina = stamina?.ToContract() ?? new StaminaSnapshot(),
                ServerTime = ParseTime(serverTime) ?? DateTimeOffset.UtcNow,
            };
        }

        [Serializable]
        private class StaminaAdLifeRewardJson
        {
            public bool granted;
            public bool duplicate;
            public int delta;
            public StaminaSnapshotJson stamina;
            public string serverTime;

            public StaminaAdLifeRewardResponse ToContract() => new StaminaAdLifeRewardResponse
            {
                Granted = granted,
                Duplicate = duplicate,
                Delta = delta,
                Stamina = stamina?.ToContract() ?? new StaminaSnapshot(),
                ServerTime = ParseTime(serverTime) ?? DateTimeOffset.UtcNow,
            };
        }

        private static DateTimeOffset? ParseTime(string value)
        {
            if (string.IsNullOrEmpty(value)) return null;
            return DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed)
                ? parsed
                : (DateTimeOffset?)null;
        }
    }
}
#pragma warning restore 0649
