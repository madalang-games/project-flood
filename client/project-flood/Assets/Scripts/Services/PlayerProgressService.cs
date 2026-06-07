using System.Collections.Generic;
using UnityEngine;

namespace Game.Services
{
    public class PlayerProgressService : MonoBehaviour
    {
        public static PlayerProgressService Instance { get; private set; }

        private const string KeyGold         = "gold";
        private const string KeyStarPrefix   = "stars_";
        private const string KeyUnlockPrefix = "unlocked_";

        private int _gold;
        private readonly Dictionary<int, int> _bestStars     = new Dictionary<int, int>();
        private readonly Dictionary<int, bool> _unlocked     = new Dictionary<int, bool>();
        private readonly Dictionary<int, int> _inventory    = new Dictionary<int, int>();

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Load();
        }

        // --- Gold ---
        public int  Gold         => _gold;
        public bool CanAfford(int cost) => _gold >= cost;

        public bool SpendGold(int cost)
        {
            if (_gold < cost) return false;
            _gold -= cost;
            PlayerPrefs.SetInt(KeyGold, _gold);
            return true;
        }

        public void AddGold(int amount)
        {
            _gold += amount;
            PlayerPrefs.SetInt(KeyGold, _gold);
        }

        public void SetGold(int amount)
        {
            _gold = amount;
            PlayerPrefs.SetInt(KeyGold, _gold);
        }

        // --- Stage stars / unlock ---
        public int GetBestStars(int stageId)
        {
            if (_bestStars.TryGetValue(stageId, out int s)) return s;
            s = PlayerPrefs.GetInt(KeyStarPrefix + stageId, 0);
            if (s > 0) _bestStars[stageId] = s;
            return s;
        }

        public bool IsStageUnlocked(int stageId)
        {
            if (_unlocked.TryGetValue(stageId, out bool u)) return u;
            u = stageId == 1 || PlayerPrefs.GetInt(KeyUnlockPrefix + stageId, 0) == 1;
            _unlocked[stageId] = u;
            return u;
        }

        public void RecordClear(int stageId, int stars)
        {
            if (stars > GetBestStars(stageId))
            {
                _bestStars[stageId] = stars;
                PlayerPrefs.SetInt(KeyStarPrefix + stageId, stars);
            }
            // unlock next stage
            UnlockStage(stageId + 1);
        }

        public void UnlockStage(int stageId)
        {
            if (_unlocked.TryGetValue(stageId, out bool v) && v) return;
            _unlocked[stageId] = true;
            PlayerPrefs.SetInt(KeyUnlockPrefix + stageId, 1);
        }

        // --- Inventory ---
        public int GetItemCount(int itemId)
        {
            _inventory.TryGetValue(itemId, out int count);
            return count;
        }

        public void SetItemCount(int itemId, int count)
        {
            _inventory[itemId] = count;
        }

        public void SetInventory(ProjectFlood.Contracts.Inventory.InventorySnapshot snapshot)
        {
            if (snapshot == null || snapshot.Items == null) return;
            foreach (var item in snapshot.Items)
            {
                _inventory[item.ItemId] = item.Count;
            }
        }

        public void ResetData()
        {
            _gold = 500;
            _bestStars.Clear();
            _unlocked.Clear();
            _unlocked[1] = true;
            _inventory.Clear();
            Debug.Log("[PlayerProgressService] Memory cache cleared.");
        }

        private void Load()
        {
            _gold = PlayerPrefs.GetInt(KeyGold, 500);
            _unlocked[1] = true; // stage 1 always unlocked
        }
    }
}
