using ProjectFlood.Data.Generated;
using Game.Utils;
using UnityEngine;

namespace Game.Services
{
    public class ItemDataService : MonoBehaviour
    {
        public static ItemDataService Instance { get; private set; }

        private Item[] _items;

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            _items = CsvLoader.Load<Item>(Item.ResourcePath);
        }

        public Item GetItem(int itemId)
        {
            foreach (var item in _items)
                if (item.item_id == itemId) return item;
            return null;
        }
    }
}
