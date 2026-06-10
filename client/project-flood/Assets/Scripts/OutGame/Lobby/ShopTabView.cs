using UnityEngine;
using UnityEngine.UI;
using Game.Core;
using Game.Core.UI;
using Game.Services;

namespace Game.OutGame.Lobby
{
    public class ShopTabView : MonoBehaviour
    {
        [SerializeField] private Button _starterPackButton;
        [SerializeField] private Button _itemBundleButton;
        [SerializeField] private Button _noAdsButton;

        private void Awake()
        {
            if (_starterPackButton != null) _starterPackButton.onClick.AddListener(() => ShowPreviewMessage("shop.starter_pack"));
            if (_itemBundleButton != null) _itemBundleButton.onClick.AddListener(() => ShowPreviewMessage("shop.item_bundle"));
            if (_noAdsButton != null) _noAdsButton.onClick.AddListener(() => ShowPreviewMessage("shop.remove_ads"));
        }

        private void ShowPreviewMessage(string titleKey)
        {
            UIManager.Instance?.ShowPopup<ConfirmDialogView>(v => v.Init(
                title: LocalizationService.Instance.Get(titleKey),
                body: LocalizationService.Instance.Get("shop.coming_soon"),
                confirmLabel: LocalizationService.Instance.Get("common.btn_ok"),
                onConfirm: null,
                onCancel: null,
                cancelLabel: ""
            ));
        }
    }
}
