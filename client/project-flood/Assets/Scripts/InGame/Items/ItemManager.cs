using System;
using System.Collections.Generic;
using Game.InGame.Board;

namespace Game.InGame.Items
{
    public class ItemManager
    {
        public bool IsInUsePhase { get; private set; }
        public ItemType? SelectedItem { get; private set; }
        public bool IsDevMode => _inventory.IsDevMode;

        // null = exited use phase; non-null = entered use phase with this item
        public event Action<ItemType?> OnUsePhaseChanged;

        private readonly ItemInventory _inventory;
        private readonly Dictionary<ItemType, IItemEffect> _effects;

        public ItemManager(ItemInventory inventory)
        {
            _inventory = inventory;
            _effects = new Dictionary<ItemType, IItemEffect>
            {
                { ItemType.Bomb,    new BombEffect() },
                { ItemType.HRocket, new HRocketEffect() },
                { ItemType.VRocket, new VRocketEffect() },
            };
        }

        public bool CanUse(ItemType type) => _inventory.CanUse(type);
        public int GetCount(ItemType type) => _inventory.GetCount(type);

        public void SelectItem(ItemType type)
        {
            if (!_inventory.CanUse(type)) return;

            if (IsInUsePhase && SelectedItem == type)
            {
                Cancel();
                return;
            }

            IsInUsePhase = true;
            SelectedItem = type;
            OnUsePhaseChanged?.Invoke(type);
        }

        public void Cancel()
        {
            if (!IsInUsePhase) return;
            IsInUsePhase = false;
            SelectedItem = null;
            OnUsePhaseChanged?.Invoke(null);
        }

        // Returns affected cells. Consumes the item and exits use phase.
        // Returns null if not in use phase.
        public List<(int row, int col)> UseItem(BoardState board, int targetRow, int targetCol)
        {
            if (!IsInUsePhase || SelectedItem == null) return null;

            var type = SelectedItem.Value;
            var cells = _effects[type].GetAffectedCells(board, targetRow, targetCol);

            _inventory.Consume(type);
            IsInUsePhase = false;
            SelectedItem = null;
            OnUsePhaseChanged?.Invoke(null);

            return cells;
        }
    }
}
