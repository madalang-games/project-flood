using System;
using ProjectFlood.Contracts.Currency;
using UnityEngine;

#pragma warning disable 0649
namespace Game.Services
{
    public class CurrencyApiService : MonoBehaviour
    {
        public static CurrencyApiService Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void FetchGold(Action<CurrencySnapshot> onSuccess = null, Action<string> onError = null)
        {
            NetworkService.Instance.Get("/api/currency", (ok, result) =>
            {
                if (!ok) { onError?.Invoke(result); return; }
                var json = JsonUtility.FromJson<CurrencySnapshotJson>(result);
                var snapshot = json.ToContract();
                PlayerProgressService.Instance?.SetGold((int)snapshot.SoftAmount);
                onSuccess?.Invoke(snapshot);
            });
        }

        public void SpendGold(int amount, string reason, Action<CurrencySnapshot> onSuccess = null, Action<string> onError = null)
        {
            var body = $"{{\"amount\":{amount},\"reason\":\"{Escape(reason)}\"}}";
            NetworkService.Instance.Post("/api/currency/spend", body, (ok, result) =>
            {
                if (!ok) { onError?.Invoke(result); return; }
                var json = JsonUtility.FromJson<CurrencySnapshotJson>(result);
                var snapshot = json.ToContract();
                PlayerProgressService.Instance?.SetGold((int)snapshot.SoftAmount);
                onSuccess?.Invoke(snapshot);
            });
        }

        private static string Escape(string value) => (value ?? string.Empty).Replace("\\", "\\\\").Replace("\"", "\\\"");

        [Serializable]
        private class CurrencySnapshotJson
        {
            public long softAmount;

            public CurrencySnapshot ToContract() => new CurrencySnapshot { SoftAmount = softAmount };
        }
    }
}
#pragma warning restore 0649
