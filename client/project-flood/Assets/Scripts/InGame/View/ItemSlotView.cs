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

        public Button Button => _button;

        public void Refresh(int count, bool isDevMode, bool canUse, bool selected)
        {
            if (_countText != null)
                _countText.text = isDevMode ? "∞" : count.ToString();
            if (_button != null)
                _button.interactable = canUse;
            if (_selectedHighlight != null)
                _selectedHighlight.SetActive(selected);
        }
    }
}
