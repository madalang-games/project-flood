using System;
using System.Globalization;
using System.Collections.Generic;
using ProjectFlood.Contracts.Stamina;
using UnityEngine;

#pragma warning disable 0649
namespace Game.Services
{
    public class StaminaApiService : MonoBehaviour
    {
        private static StaminaApiService _instance;

        public static StaminaApiService Instance
        {
            get
            {
                if (_instance == null)
                {
                    Debug.LogError("[StaminaApiService] Instance is missing! Ensure it is placed in the Boot scene.");
                }
                return _instance;
            }
        }

        public event Action OnStaminaUpdated;

        private StaminaSnapshot _latestStamina;
        private DateTimeOffset  _serverTimeAtLatestUpdate;
        private float           _localTimeAtLatestUpdate;

        public StaminaSnapshot LatestStamina => _latestStamina;

        private void Awake()
        {
            if (_instance != null && _instance != this) { Destroy(gameObject); return; }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void UpdateStamina(StaminaSnapshot snapshot, DateTimeOffset serverTime)
        {
            _latestStamina            = snapshot;
            _serverTimeAtLatestUpdate = serverTime;
            _localTimeAtLatestUpdate  = Time.realtimeSinceStartup;
            OnStaminaUpdated?.Invoke();
        }

        public void FetchStamina(Action<StaminaStatusResponse> onSuccess = null, Action<string> onError = null)
        {
            NetworkService.Instance.Get("/api/stamina", NetworkRetryOptions.LobbyAndSave, (ok, result) =>
            {
                if (!ok) { onError?.Invoke(result); return; }
                var json     = JsonUtility.FromJson<StaminaStatusJson>(result);
                var response = json.ToContract();
                UpdateStamina(response.Stamina, response.ServerTime);
                onSuccess?.Invoke(response);
            });
        }

        public void ClaimAdLife(string provider, string adToken, Action<StaminaAdLifeRewardResponse> onSuccess = null, Action<string> onError = null)
        {
            StartCoroutine(PollClaimAdLife(provider, adToken, 0, onSuccess, onError));
        }

        private System.Collections.IEnumerator PollClaimAdLife(string provider, string adToken, int attempt, Action<StaminaAdLifeRewardResponse> onSuccess, Action<string> onError)
        {
            if (attempt == 0)
            {
                var body = $"{{\"provider\":\"{Escape(provider)}\",\"adToken\":\"{Escape(adToken)}\"}}";
                bool complete = false;
                string errorText = null;
                StaminaAdLifeRewardResponse response = null;

                NetworkService.Instance.Post("/api/stamina/ad-life-reward", body, (ok, result) =>
                {
                    if (ok)
                    {
                        var json = JsonUtility.FromJson<StaminaAdLifeRewardJson>(result);
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
                    UpdateStamina(response.Stamina, response.ServerTime);
                    onSuccess?.Invoke(response);
                    yield break;
                }

                string code = null;
                try
                {
                    var err = JsonUtility.FromJson<ErrorResponseJson>(errorText);
                    code = err?.code;
                }
                catch {}

                if (code == "AD_SSV_PENDING")
                {
                    yield return new WaitForSeconds(1.0f);
                    StartCoroutine(PollClaimAdLife(provider, adToken, attempt + 1, onSuccess, onError));
                }
                else
                {
                    onError?.Invoke(errorText);
                }
            }
            else
            {
                bool complete = false;
                string errorText = null;
                string resultText = null;
                bool isSuccess = false;

                NetworkService.Instance.Get($"/api/ad-rewards/status/{adToken}", NetworkRetryOptions.LobbyAndSave, (ok, result) =>
                {
                    isSuccess = ok;
                    if (ok) resultText = result;
                    else errorText = result;
                    complete = true;
                });

                yield return new WaitUntil(() => complete);

                if (isSuccess && resultText != null)
                {
                    var statusResponse = JsonUtility.FromJson<AdRewardStatusJson>(resultText);
                    if (statusResponse != null && statusResponse.status == "GRANTED")
                    {
                        var staminaSnapshot = statusResponse.stamina?.ToContract() ?? new StaminaSnapshot();
                        var serverTime = ParseTime(statusResponse.serverTime) ?? DateTimeOffset.UtcNow;
                        UpdateStamina(staminaSnapshot, serverTime);

                        var rewardResponse = new StaminaAdLifeRewardResponse
                        {
                            Granted = true,
                            Duplicate = false,
                            Delta = statusResponse.GrantedLifeAmount(),
                            Stamina = staminaSnapshot,
                            ServerTime = serverTime
                        };
                        onSuccess?.Invoke(rewardResponse);
                    }
                    else if (attempt < 10)
                    {
                        yield return new WaitForSeconds(1.0f);
                        StartCoroutine(PollClaimAdLife(provider, adToken, attempt + 1, onSuccess, onError));
                    }
                    else
                    {
                        onError?.Invoke("Ad verification timeout.");
                    }
                }
                else
                {
                    onError?.Invoke(errorText);
                }
            }
        }

        public int GetEstimatedLife()
        {
            if (_latestStamina == null) return 5;
            if (_latestStamina.IsUnlimited) return _latestStamina.Current;

            if (_latestStamina.Current >= _latestStamina.Max)
                return _latestStamina.Current;

            if (!_latestStamina.NextRechargeAt.HasValue)
                return _latestStamina.Current;

            var elapsed           = Time.realtimeSinceStartup - _localTimeAtLatestUpdate;
            var currentServerTime = _serverTimeAtLatestUpdate.AddSeconds(elapsed);
            var rechargeTime      = _latestStamina.NextRechargeAt.Value;

            if (currentServerTime >= rechargeTime)
            {
                var secondsSinceRecharge = (currentServerTime - rechargeTime).TotalSeconds;
                var extraLives           = 1 + (int)(secondsSinceRecharge / _latestStamina.RegenSeconds);
                return Math.Min(_latestStamina.Max, _latestStamina.Current + extraLives);
            }

            return _latestStamina.Current;
        }

        public double GetSecondsToNextRecharge()
        {
            if (_latestStamina == null || _latestStamina.Current >= _latestStamina.Max || !_latestStamina.NextRechargeAt.HasValue)
                return 0;

            var elapsed           = Time.realtimeSinceStartup - _localTimeAtLatestUpdate;
            var currentServerTime = _serverTimeAtLatestUpdate.AddSeconds(elapsed);
            var rechargeTime      = _latestStamina.NextRechargeAt.Value;

            if (currentServerTime >= rechargeTime)
            {
                var secondsSinceRecharge  = (currentServerTime - rechargeTime).TotalSeconds;
                var remainingInPeriod     = _latestStamina.RegenSeconds - (secondsSinceRecharge % _latestStamina.RegenSeconds);
                return remainingInPeriod;
            }

            return (rechargeTime - currentServerTime).TotalSeconds;
        }

        public double GetSecondsOfUnlimitedRemaining()
        {
            if (_latestStamina == null || !_latestStamina.IsUnlimited || !_latestStamina.UnlimitedUntil.HasValue)
                return 0;

            var elapsed           = Time.realtimeSinceStartup - _localTimeAtLatestUpdate;
            var currentServerTime = _serverTimeAtLatestUpdate.AddSeconds(elapsed);
            var unlimitedTime     = _latestStamina.UnlimitedUntil.Value;

            if (currentServerTime >= unlimitedTime)
                return 0;

            return (unlimitedTime - currentServerTime).TotalSeconds;
        }

        private static string Escape(string value) => (value ?? string.Empty).Replace("\\", "\\\\").Replace("\"", "\\\"");

        [Serializable]
        private class StaminaSnapshotJson
        {
            public int    current;
            public int    max;
            public string nextRechargeAt;
            public string unlimitedUntil;
            public bool   isUnlimited;
            public int    regenSeconds;

            public StaminaSnapshot ToContract() => new StaminaSnapshot
            {
                Current        = current,
                Max            = max,
                NextRechargeAt = ParseTime(nextRechargeAt),
                UnlimitedUntil = ParseTime(unlimitedUntil),
                IsUnlimited    = isUnlimited,
                RegenSeconds   = regenSeconds,
            };
        }

        [Serializable]
        private class StaminaStatusJson
        {
            public StaminaSnapshotJson stamina;
            public string              serverTime;

            public StaminaStatusResponse ToContract() => new StaminaStatusResponse
            {
                Stamina    = stamina?.ToContract() ?? new StaminaSnapshot(),
                ServerTime = ParseTime(serverTime) ?? DateTimeOffset.UtcNow,
            };
        }

        [Serializable]
        private class StaminaAdLifeRewardJson
        {
            public bool                granted;
            public bool                duplicate;
            public int                 delta;
            public StaminaSnapshotJson stamina;
            public string              serverTime;

            public StaminaAdLifeRewardResponse ToContract() => new StaminaAdLifeRewardResponse
            {
                Granted    = granted,
                Duplicate  = duplicate,
                Delta      = delta,
                Stamina    = stamina?.ToContract() ?? new StaminaSnapshot(),
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

        [Serializable]
        private class AdRewardStatusJson
        {
            public string status;
            public string placementId;
            public List<GrantedRewardJson> grantedRewards;
            public StaminaSnapshotJson stamina;
            public string serverTime;

            public int GrantedLifeAmount()
            {
                if (grantedRewards == null) return 0;
                foreach (var r in grantedRewards)
                {
                    if (r.rewardType == "STAMINA_LIFE")
                        return r.amount;
                }
                return 0;
            }
        }

        [Serializable]
        private class GrantedRewardJson
        {
            public string rewardType;
            public int targetId;
            public int amount;
            public int durationSeconds;
        }
    }
}
#pragma warning restore 0649
