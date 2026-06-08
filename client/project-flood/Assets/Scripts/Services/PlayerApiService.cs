using System;
using System.Collections.Generic;
using ProjectFlood.Contracts.Player;
using UnityEngine;

#pragma warning disable 0649
namespace Game.Services
{
    public class PlayerApiService : MonoBehaviour
    {
        public static PlayerApiService Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void FetchProgress(Action<bool, PlayerProgressResponse> onComplete)
        {
            NetworkService.Instance.Get("/api/player/progress", NetworkRetryOptions.LobbyAndSave, (ok, text) =>
            {
                if (!ok) { onComplete?.Invoke(false, null); return; }
                try
                {
                    var json = JsonUtility.FromJson<PlayerProgressJson>(text);
                    onComplete?.Invoke(true, json?.ToContract());
                }
                catch
                {
                    onComplete?.Invoke(false, null);
                }
            });
        }

        [Serializable]
        private class PlayerProgressJson
        {
            public int maxClearedStageId;
            public StageProgressEntryJson[] stages;

            public PlayerProgressResponse ToContract()
            {
                var response = new PlayerProgressResponse
                {
                    MaxClearedStageId = maxClearedStageId,
                    Stages = new List<StageProgressEntry>(),
                };
                if (stages != null)
                    foreach (var s in stages)
                        response.Stages.Add(s.ToContract());
                return response;
            }
        }

        [Serializable]
        private class StageProgressEntryJson
        {
            public int stageId;
            public int bestStar;

            public StageProgressEntry ToContract()
                => new StageProgressEntry { StageId = stageId, BestStar = bestStar };
        }
    }
}
#pragma warning restore 0649
