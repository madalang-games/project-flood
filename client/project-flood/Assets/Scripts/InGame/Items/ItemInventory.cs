using System.Collections.Generic;

namespace Game.InGame.Items
{
    public class ItemInventory
    {
        public bool IsDevMode;

        private readonly Dictionary<ItemType, int> _counts = new Dictionary<ItemType, int>
        {
            { ItemType.Bomb,    0 },
            { ItemType.HRocket, 0 },
            { ItemType.VRocket, 0 },
        };

        public bool CanUse(ItemType type) => IsDevMode || GetCount(type) > 0;

        public void Consume(ItemType type)
        {
            if (!IsDevMode && _counts.ContainsKey(type))
                _counts[type]--;
        }

        public int GetCount(ItemType type)
        {
            _counts.TryGetValue(type, out int count);
            return count;
        }
    }
}
