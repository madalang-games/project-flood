using UnityEngine;
using UnityEngine.UI;
using Game.Core;
using Game.Core.UI;

namespace Game.OutGame.Lobby
{
    public class ShopTabView : MonoBehaviour
    {
        [SerializeField] private Button _starterPackButton;
        [SerializeField] private Button _itemBundleButton;
        [SerializeField] private Button _noAdsButton;

        private void Awake()
        {
            if (_starterPackButton != null) _starterPackButton.onClick.AddListener(() => ShowPreviewMessage("Starter Pack"));
            if (_itemBundleButton != null) _itemBundleButton.onClick.AddListener(() => ShowPreviewMessage("Item Bundle"));
            if (_noAdsButton != null) _noAdsButton.onClick.AddListener(() => ShowPreviewMessage("Remove Ads"));
        }

        private void ShowPreviewMessage(string packageName)
        {
            UIManager.Instance?.ShowPopup<ConfirmDialogView>(v => v.Init(
                title: packageName,
                body: "This package will be available for In-App Purchase in a future update. Coming soon!",
                confirmLabel: "OK",
                onConfirm: null,
                onCancel: null,
                cancelLabel: ""
            ));
        }
    }
}
