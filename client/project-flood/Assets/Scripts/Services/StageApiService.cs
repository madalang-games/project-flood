using System;
using System.Collections;
using System.Globalization;
using ProjectFlood.Contracts.Stage;
using UnityEngine;
using UnityEngine.Networking;

#pragma warning disable 0649
namespace Game.Services
{
    public class StageApiService : MonoBehaviour
    {
        public static StageApiService Instance { get; private set; }

        [SerializeField] private string _baseUrl = "http://localhost:5000";
        [SerializeField] private string _authToken;

        private StageAttemptSnapshot _currentAttempt;

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void SetAuthToken(string authToken) => _authToken = authToken;

        public bool HasAttemptFor(int stageId)
            => _currentAttempt != null && _currentAttempt.StageId == stageId && !string.IsNullOrEmpty(_currentAttempt.AttemptId);

        public void StartAttempt(int stageId, Action<StageAttemptStartResponse> onSuccess = null, Action<string> onError = null)
            => StartCoroutine(Post($"{_baseUrl}/api/stages/{stageId}/attempts/start", "{\"clientRequestId\":\"\"}", text =>
            {
                var json = JsonUtility.FromJson<StageAttemptStartJson>(text);
                var response = json.ToContract();
                _currentAttempt = response.Attempt;
                onSuccess?.Invoke(response);
            }, onError));

        public void ClearAttempt(int stageId, StageAttemptClearRequest request, Action<StageAttemptEndResponse> onSuccess = null, Action<string> onError = null)
        {
            if (!HasAttemptFor(stageId))
                return;

            var body = "{"
                + $"\"clientRequestId\":\"{Escape(request.ClientRequestId)}\","
                + $"\"rulesetVersion\":{request.RulesetVersion},"
                + $"\"turnsUsed\":{request.TurnsUsed},"
                + $"\"remainingBasicCells\":{request.RemainingBasicCells},"
                + $"\"coreRemaining\":{request.CoreRemaining.ToString().ToLowerInvariant()}"
                + "}";

            StartCoroutine(Post($"{_baseUrl}/api/stages/{stageId}/attempts/{_currentAttempt.AttemptId}/clear", body, text =>
            {
                var json = JsonUtility.FromJson<StageAttemptEndJson>(text);
                _currentAttempt = null;
                onSuccess?.Invoke(json.ToContract());
            }, onError));
        }

        public void FailAttempt(int stageId, string reason = "fail")
        {
            if (!HasAttemptFor(stageId))
                return;

            var body = $"{{\"clientRequestId\":\"\",\"reason\":\"{Escape(reason)}\"}}";
            StartCoroutine(Post($"{_baseUrl}/api/stages/{stageId}/attempts/{_currentAttempt.AttemptId}/fail", body, _ => _currentAttempt = null, _ => { }));
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
        private class StageAttemptSnapshotJson
        {
            public string attemptId;
            public int stageId;
            public string expiresAt;
            public int reviveCount;
            public int remainingRevives;
            public bool lifeSpent;

            public StageAttemptSnapshot ToContract() => new StageAttemptSnapshot
            {
                AttemptId = attemptId ?? string.Empty,
                StageId = stageId,
                ExpiresAt = ParseTime(expiresAt),
                ReviveCount = reviveCount,
                RemainingRevives = remainingRevives,
                LifeSpent = lifeSpent,
            };
        }

        [Serializable]
        private class StageAttemptStartJson
        {
            public StageAttemptSnapshotJson attempt;
            public string serverTime;

            public StageAttemptStartResponse ToContract() => new StageAttemptStartResponse
            {
                Attempt = attempt?.ToContract() ?? new StageAttemptSnapshot(),
                ServerTime = ParseTime(serverTime),
            };
        }

        [Serializable]
        private class StageAttemptEndJson
        {
            public string attemptId;
            public int stageId;
            public string result;
            public bool lifeRefunded;
            public int stars;
            public int turnsUsed;
            public int stageRank;
            public bool isNewBest;
            public string serverTime;

            public StageAttemptEndResponse ToContract() => new StageAttemptEndResponse
            {
                AttemptId = attemptId ?? string.Empty,
                StageId = stageId,
                Result = result ?? string.Empty,
                LifeRefunded = lifeRefunded,
                Stars = stars,
                TurnsUsed = turnsUsed,
                StageRank = stageRank > 0 ? stageRank : null,
                IsNewBest = isNewBest,
                ServerTime = ParseTime(serverTime),
            };
        }

        private static DateTimeOffset ParseTime(string value)
            => DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed)
                ? parsed
                : DateTimeOffset.MinValue;
    }
}
#pragma warning restore 0649
