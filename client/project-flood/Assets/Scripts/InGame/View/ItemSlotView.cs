using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.InGame.View
{
    public class ItemSlotView : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private TextMeshProUGUI _countText;
        [SerializeField] private GameObject _selectedHighlight;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private GameObject _goldBadge;
        [SerializeField] private TextMeshProUGUI _goldBadgeText;
        [SerializeField] private Image _icon;

        public Button Button => _button;
        public Image Icon => _icon;

        public void Refresh(int count, bool isDevMode, bool canUse, bool selected, int goldCost = 100)
        {
            bool isZero = !isDevMode && count == 0;

            if (_countText != null)
            {
                _countText.text = isDevMode ? "∞" : count.ToString();
                _countText.gameObject.SetActive(!isZero);
            }

            if (_goldBadge != null)
            {
                _goldBadge.SetActive(isZero);
            }

            if (_goldBadgeText != null && isZero)
            {
                _goldBadgeText.text = goldCost.ToString();
            }

            if (_button != null)
            {
                // Can tap to buy with gold if count is 0 and not locked
                _button.interactable = canUse || isZero;
            }

            if (_selectedHighlight != null)
                _selectedHighlight.SetActive(selected);

            if (_canvasGroup != null)
            {
                // Maintain full opacity for gold badge visibility when zero
                _canvasGroup.alpha = 1f;
            }
        }
    }
}
