using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.InGame.View
{
    public class ItemBuyConfirmPopupView : MonoBehaviour
    {
        [SerializeField] private Image _icon;
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private TMP_Text _descText;
        [SerializeField] private TMP_Text _priceText;
        [SerializeField] private TMP_Text _ownedGoldText;
        [SerializeField] private Button _confirmButton;
        [SerializeField] private Button _cancelButton;
        [SerializeField] private Button _backdropButton;

        private Action _onConfirm;

        public void Init(Sprite icon, string itemName, string itemDesc, int price, int ownedGold, Action onConfirm)
        {
            if (_icon != null && icon != null) _icon.sprite = icon;
            if (_nameText != null) _nameText.text = itemName;
            if (_descText != null) _descText.text = itemDesc;

            var loc = Services.LocalizationService.Instance;
            if (_priceText != null)
                _priceText.text = string.Format(loc.Get("popup.buy.cost_fmt"), price);
            if (_ownedGoldText != null)
                _ownedGoldText.text = string.Format(loc.Get("popup.buy.gold_fmt"), ownedGold);

            if (_confirmButton != null) _confirmButton.interactable = ownedGold >= price;

            _onConfirm = onConfirm;

            _confirmButton?.onClick.AddListener(OnConfirm);
            _cancelButton?.onClick.AddListener(OnCancel);
            _backdropButton?.onClick.AddListener(OnCancel);
        }

        private void OnConfirm()
        {
            _onConfirm?.Invoke();
            Close();
        }

        private void OnCancel() => Close();

        private void Close()
        {
            var appear = GetComponent<Core.UI.UIPanelAppear>();
            if (appear != null)
                appear.Disappear(() => Core.UIManager.Instance?.CloseTopPopup());
            else
                Core.UIManager.Instance?.CloseTopPopup();
        }
    }
}
