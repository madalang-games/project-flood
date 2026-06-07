using System;
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

        private void Awake()
        {
            _bombSlot.Button.onClick.AddListener(() => OnSlotTapped?.Invoke(ItemType.Bomb));
            _hRocketSlot.Button.onClick.AddListener(() => OnSlotTapped?.Invoke(ItemType.HRocket));
            _colorSweepSlot.Button.onClick.AddListener(() => OnSlotTapped?.Invoke(ItemType.ColorSweep));
            _rowShiftSlot.Button.onClick.AddListener(() => OnSlotTapped?.Invoke(ItemType.RowShift));
            _cellSwapSlot.Button.onClick.AddListener(() => OnSlotTapped?.Invoke(ItemType.CellSwap));
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

        private void RefreshSlot(ItemSlotView slot, ItemType type, ItemManager manager)
        {
            if (slot == null) return;
            bool canUse = !_isLocked && manager.CanUse(type);
            bool selected = manager.IsInUsePhase && manager.SelectedItem == type;
            slot.Refresh(manager.GetCount(type), manager.IsDevMode, canUse, selected);
        }
    }
}
