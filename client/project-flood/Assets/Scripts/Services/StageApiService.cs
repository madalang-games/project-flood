using System;
using System.Globalization;
using ProjectFlood.Contracts.Stage;
using UnityEngine;

#pragma warning disable 0649
namespace Game.Services
{
    public class StageApiService : MonoBehaviour
    {
        private static StageApiService _instance;

        public static StageApiService Instance
        {
            get
            {
                if (_instance == null)
                {
                    Debug.LogError("[StageApiService] Instance is missing! Ensure it is placed in the Boot scene.");
                }
                return _instance;
            }
        }

        private StageAttemptSnapshot _currentAttempt;
        public StageAttemptSnapshot CurrentAttempt => _currentAttempt;

        private void Awake()
        {
            if (_instance != null && _instance != this) { Destroy(gameObject); return; }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public bool HasAttemptFor(int stageId)
            => _currentAttempt != null && _currentAttempt.StageId == stageId && !string.IsNullOrEmpty(_currentAttempt.AttemptId);

        public void StartAttempt(int stageId, Action<StageAttemptStartResponse> onSuccess = null, Action<string> onError = null)
        {
            var requestId = Guid.NewGuid().ToString("N");
            Debug.Log($"[StageApiService] StartAttempt: stageId={stageId}, requestId={requestId}");
            var body = $"{{\"clientRequestId\":\"{requestId}\"}}";
            NetworkService.Instance.Post($"/api/stages/{stageId}/attempts/start", body, (ok, result) =>
            {
                if (!ok) 
                { 
                    string errorCode = null;
                    try { errorCode = JsonUtility.FromJson<ErrorResponseJson>(result)?.code; } catch { }
                    onError?.Invoke(errorCode ?? result); 
                    return; 
                }
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

        public void ReviveAd(int stageId, string attemptId, string provider, string adToken, Action<StageReviveAdResponse> onSuccess = null, Action<string> onError = null)
        {
            StartCoroutine(PollReviveAd(stageId, attemptId, provider, adToken, 0, onSuccess, onError));
        }

        private System.Collections.IEnumerator PollReviveAd(int stageId, string attemptId, string provider, string adToken, int attempt, Action<StageReviveAdResponse> onSuccess, Action<string> onError)
        {
            var body = $"{{\"provider\":\"{Escape(provider)}\",\"adToken\":\"{Escape(adToken)}\"}}";
            bool complete = false;
            string errorText = null;
            StageReviveAdResponse response = null;

            NetworkService.Instance.Post($"/api/stages/{stageId}/attempts/{attemptId}/revive-ad", body, (ok, result) =>
            {
                if (ok)
                {
                    var json = JsonUtility.FromJson<StageReviveAdJson>(result);
                    response = json.ToContract();
                }
                else
                {
                    errorText = result;
                }
                complete = true;
            });

            yield return new WaitUntil(() => complete);

            if (response != null)
            {
                _currentAttempt = response.Attempt;
                onSuccess?.Invoke(response);
            }
            else
            {
                string code = null;
                try
                {
                    var err = JsonUtility.FromJson<ErrorResponseJson>(errorText);
                    code = err?.code;
                }
                catch {}

                if (code == "AD_SSV_PENDING" && attempt < 10)
                {
                    yield return new WaitForSeconds(1.0f);
                    StartCoroutine(PollReviveAd(stageId, attemptId, provider, adToken, attempt + 1, onSuccess, onError));
                }
                else
                {
                    onError?.Invoke(errorText);
                }
            }
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
        private class CurrencySnapshotJson
        {
            public long softAmount;

            public ProjectFlood.Contracts.Currency.CurrencySnapshot ToContract()
                => new ProjectFlood.Contracts.Currency.CurrencySnapshot { SoftAmount = softAmount };
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
            public CurrencySnapshotJson currency;

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
                Currency     = currency?.ToContract(),
            };
        }

        [Serializable]
        private class StageReviveAdJson
        {
            public bool granted;
            public bool duplicate;
            public int reviveCount;
            public int turnsGranted;
            public StageAttemptSnapshotJson attempt;
            public string serverTime;

            public StageReviveAdResponse ToContract() => new StageReviveAdResponse
            {
                Granted = granted,
                Duplicate = duplicate,
                ReviveCount = reviveCount,
                TurnsGranted = turnsGranted,
                Attempt = attempt?.ToContract() ?? new StageAttemptSnapshot(),
                ServerTime = ParseTime(serverTime)
            };
        }

        private static DateTimeOffset ParseTime(string value)
            => DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed)
                ? parsed
                : DateTimeOffset.MinValue;
    }
}
#pragma warning restore 0649
