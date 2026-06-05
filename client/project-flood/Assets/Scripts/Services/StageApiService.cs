using System;
using System.Globalization;
using ProjectFlood.Contracts.Stage;
using UnityEngine;

#pragma warning disable 0649
namespace Game.Services
{
    public class StageApiService : MonoBehaviour
    {
        public static StageApiService Instance { get; private set; }

        private StageAttemptSnapshot _currentAttempt;

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public bool HasAttemptFor(int stageId)
            => _currentAttempt != null && _currentAttempt.StageId == stageId && !string.IsNullOrEmpty(_currentAttempt.AttemptId);

        public void StartAttempt(int stageId, Action<StageAttemptStartResponse> onSuccess = null, Action<string> onError = null)
        {
            NetworkService.Instance.Post($"/api/stages/{stageId}/attempts/start", "{\"clientRequestId\":\"\"}", (ok, result) =>
            {
                if (!ok) { onError?.Invoke(result); return; }
                var json     = JsonUtility.FromJson<StageAttemptStartJson>(result);
                var response = json.ToContract();
                _currentAttempt = response.Attempt;
                onSuccess?.Invoke(response);
            });
        }

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

            NetworkService.Instance.Post($"/api/stages/{stageId}/attempts/{_currentAttempt.AttemptId}/clear", body, (ok, result) =>
            {
                if (!ok) { onError?.Invoke(result); return; }
                var json = JsonUtility.FromJson<StageAttemptEndJson>(result);
                _currentAttempt = null;
                onSuccess?.Invoke(json.ToContract());
            });
        }

        public void FailAttempt(int stageId, string reason = "fail")
        {
            if (!HasAttemptFor(stageId))
                return;

            var body = $"{{\"clientRequestId\":\"\",\"reason\":\"{Escape(reason)}\"}}";
            NetworkService.Instance.Post($"/api/stages/{stageId}/attempts/{_currentAttempt.AttemptId}/fail", body, (ok, _) =>
            {
                if (ok) _currentAttempt = null;
            });
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
                AttemptId       = attemptId ?? string.Empty,
                StageId         = stageId,
                ExpiresAt       = ParseTime(expiresAt),
                ReviveCount     = reviveCount,
                RemainingRevives = remainingRevives,
                LifeSpent       = lifeSpent,
            };
        }

        [Serializable]
        private class StageAttemptStartJson
        {
            public StageAttemptSnapshotJson attempt;
            public string serverTime;

            public StageAttemptStartResponse ToContract() => new StageAttemptStartResponse
            {
                Attempt    = attempt?.ToContract() ?? new StageAttemptSnapshot(),
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
                AttemptId    = attemptId ?? string.Empty,
                StageId      = stageId,
                Result       = result ?? string.Empty,
                LifeRefunded = lifeRefunded,
                Stars        = stars,
                TurnsUsed    = turnsUsed,
                StageRank    = stageRank > 0 ? stageRank : null,
                IsNewBest    = isNewBest,
                ServerTime   = ParseTime(serverTime),
            };
        }

        private static DateTimeOffset ParseTime(string value)
            => DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed)
                ? parsed
                : DateTimeOffset.MinValue;
    }
}
#pragma warning restore 0649
