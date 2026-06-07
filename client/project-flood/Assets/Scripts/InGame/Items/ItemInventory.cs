using System;
using System.Collections.Generic;
using Game.Services;

namespace Game.InGame.Items
{
    public class ItemInventory
    {
        public bool IsDevMode;

        private static int ToItemId(ItemType type)
        {
            return type switch
            {
                ItemType.Bomb => 2,
                ItemType.HRocket => 3,
                ItemType.ColorSweep => 4,
                ItemType.RowShift => 5,
                ItemType.CellSwap => 6,
                _ => -1
            };
        }

        public bool CanUse(ItemType type) => IsDevMode || GetCount(type) > 0;

        public void Consume(ItemType type)
        {
            int itemId = ToItemId(type);
            if (itemId > 0 && PlayerProgressService.Instance != null)
            {
                int current = PlayerProgressService.Instance.GetItemCount(itemId);
                PlayerProgressService.Instance.SetItemCount(itemId, Math.Max(0, current - 1));
            }
        }

        public int GetCount(ItemType type)
        {
            int itemId = ToItemId(type);
            if (itemId > 0 && PlayerProgressService.Instance != null)
            {
                return PlayerProgressService.Instance.GetItemCount(itemId);
            }
            return 0;
        }
    }
}
