using System;
using System.Collections.Generic;
using Game.InGame.Items;
using UnityEngine;

namespace Game.InGame.View
{
    public class ItemTrayView : MonoBehaviour
    {
        [SerializeField] private ItemSlotView _bombSlot;
        [SerializeField] private ItemSlotView _hRocketSlot;
        [SerializeField] private ItemSlotView _colorSweepSlot;
        [SerializeField] private ItemSlotView _rowShiftSlot;
        [SerializeField] private ItemSlotView _cellSwapSlot;

        public event Action<ItemType> OnSlotTapped;

        private bool _isLocked;
        private readonly Dictionary<ItemType, int> _prices = new();

        private void Awake()
        {
            _bombSlot.Button.onClick.AddListener(() => OnSlotTapped?.Invoke(ItemType.Bomb));
            _hRocketSlot.Button.onClick.AddListener(() => OnSlotTapped?.Invoke(ItemType.HRocket));
            _colorSweepSlot.Button.onClick.AddListener(() => OnSlotTapped?.Invoke(ItemType.ColorSweep));
            _rowShiftSlot.Button.onClick.AddListener(() => OnSlotTapped?.Invoke(ItemType.RowShift));
            _cellSwapSlot.Button.onClick.AddListener(() => OnSlotTapped?.Invoke(ItemType.CellSwap));
        }

        public void SetItemPrices(Dictionary<ItemType, int> prices)
        {
            _prices.Clear();
            foreach (var kv in prices) _prices[kv.Key] = kv.Value;
        }

        public void Refresh(ItemManager manager)
        {
            RefreshSlot(_bombSlot,       ItemType.Bomb,       manager);
            RefreshSlot(_hRocketSlot,    ItemType.HRocket,    manager);
            RefreshSlot(_colorSweepSlot, ItemType.ColorSweep, manager);
            RefreshSlot(_rowShiftSlot,   ItemType.RowShift,   manager);
            RefreshSlot(_cellSwapSlot,   ItemType.CellSwap,   manager);
        }

        public void SetLocked(bool locked)
        {
            _isLocked = locked;
        }

        public Sprite GetSlotSprite(ItemType type) => GetSlot(type)?.Icon?.sprite;

        private ItemSlotView GetSlot(ItemType type) => type switch
        {
            ItemType.Bomb       => _bombSlot,
            ItemType.HRocket    => _hRocketSlot,
            ItemType.ColorSweep => _colorSweepSlot,
            ItemType.RowShift   => _rowShiftSlot,
            ItemType.CellSwap   => _cellSwapSlot,
            _                   => null
        };

        private void RefreshSlot(ItemSlotView slot, ItemType type, ItemManager manager)
        {
            if (slot == null) return;
            bool canUse = !_isLocked && (manager.CanUse(type) || manager.GetCount(type) == 0);
            bool selected = manager.IsInUsePhase && manager.SelectedItem == type;
            int goldCost = _prices.TryGetValue(type, out var p) ? p : 100;
            slot.Refresh(manager.GetCount(type), manager.IsDevMode, canUse, selected, goldCost);
        }
    }
}
