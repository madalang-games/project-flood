using System;
using System.Collections;
using System.Collections.Generic;
using ProjectFlood.Contracts.Ranking;
using UnityEngine;
using UnityEngine.Networking;

#pragma warning disable 0649
namespace Game.Services
{
    public class RankingApiService : MonoBehaviour
    {
        public static RankingApiService Instance { get; private set; }

        [SerializeField] private string _baseUrl = "http://localhost:5000";
        [SerializeField] private string _authToken;

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void SetAuthToken(string authToken) => _authToken = authToken;

        public void FetchGlobalPage(string rankingType, int offset, int limit, Action<RankingPageResponse> onSuccess, Action<string> onError = null)
            => StartCoroutine(Get($"{_baseUrl}/api/rankings/global/{rankingType}?offset={offset}&limit={limit}", text =>
            {
                var json = JsonUtility.FromJson<RankingPageJson>(text);
                onSuccess?.Invoke(json.ToContract());
            }, onError));

        public void FetchMyGlobalRank(string rankingType, Action<MyRankingResponse> onSuccess, Action<string> onError = null)
            => StartCoroutine(Get($"{_baseUrl}/api/rankings/global/{rankingType}/me", text =>
            {
                var json = JsonUtility.FromJson<MyRankingJson>(text);
                onSuccess?.Invoke(json.ToContract());
            }, onError));

        public void FetchMyStageRank(int stageId, Action<StageRankResponse> onSuccess, Action<string> onError = null)
            => StartCoroutine(Get($"{_baseUrl}/api/rankings/stages/{stageId}/me", text =>
            {
                var json = JsonUtility.FromJson<StageRankJson>(text);
                onSuccess?.Invoke(json.ToContract());
            }, onError));

        private IEnumerator Get(string url, Action<string> onSuccess, Action<string> onError)
        {
            using var req = UnityWebRequest.Get(url);
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

        [Serializable]
        private class RankingEntryJson
        {
            public long userId;
            public string displayName;
            public int avatarId;
            public int rank;
            public int score;

            public RankingEntryDto ToContract() => new RankingEntryDto
            {
                UserId = userId,
                DisplayName = displayName ?? string.Empty,
                AvatarId = avatarId,
                Rank = rank,
                Score = score,
            };
        }

        [Serializable]
        private class RankingPageJson
        {
            public string rankingType;
            public int offset;
            public int limit;
            public List<RankingEntryJson> entries;

            public RankingPageResponse ToContract()
            {
                var response = new RankingPageResponse
                {
                    RankingType = rankingType ?? string.Empty,
                    Offset = offset,
                    Limit = limit,
                };

                if (entries != null)
                    foreach (var entry in entries)
                        if (entry != null)
                            response.Entries.Add(entry.ToContract());

                return response;
            }
        }

        [Serializable]
        private class MyRankingJson
        {
            public string rankingType;
            public RankingEntryJson entry;

            public MyRankingResponse ToContract() => new MyRankingResponse
            {
                RankingType = rankingType ?? string.Empty,
                Entry = entry?.ToContract(),
            };
        }

        [Serializable]
        private class StageRankJson
        {
            public int stageId;
            public int rank;
            public int bestTurnsUsed;

            public StageRankResponse ToContract() => new StageRankResponse
            {
                StageId = stageId,
                Rank = rank > 0 ? rank : null,
                BestTurnsUsed = bestTurnsUsed > 0 ? bestTurnsUsed : null,
            };
        }
    }
}
#pragma warning restore 0649
