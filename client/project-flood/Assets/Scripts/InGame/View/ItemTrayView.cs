using System;
using Game.InGame.Items;
using UnityEngine;

namespace Game.InGame.View
{
    public class ItemTrayView : MonoBehaviour
    {
        [SerializeField] private ItemSlotView _bombSlot;
        [SerializeField] private ItemSlotView _hRocketSlot;
        [SerializeField] private ItemSlotView _vRocketSlot;

        public event Action<ItemType> OnSlotTapped;

        private bool _isLocked;

        private void Awake()
        {
            _bombSlot.Button.onClick.AddListener(() => OnSlotTapped?.Invoke(ItemType.Bomb));
            _hRocketSlot.Button.onClick.AddListener(() => OnSlotTapped?.Invoke(ItemType.HRocket));
            _vRocketSlot.Button.onClick.AddListener(() => OnSlotTapped?.Invoke(ItemType.VRocket));
        }

        public void Refresh(ItemManager manager)
        {
            RefreshSlot(_bombSlot,    ItemType.Bomb,    manager);
            RefreshSlot(_hRocketSlot, ItemType.HRocket, manager);
            RefreshSlot(_vRocketSlot, ItemType.VRocket, manager);
        }

        public void SetLocked(bool locked)
        {
            _isLocked = locked;
        }

        private void RefreshSlot(ItemSlotView slot, ItemType type, ItemManager manager)
        {
            bool canUse = !_isLocked && manager.CanUse(type);
            bool selected = manager.IsInUsePhase && manager.SelectedItem == type;
            slot.Refresh(manager.GetCount(type), manager.IsDevMode, canUse, selected);
        }
    }
}
