using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.OutGame.Lobby
{
    public class HeaderView : MonoBehaviour
    {
        [SerializeField] private Button   _avatarButton;
        [SerializeField] private TMP_Text _goldText;

        private void Awake()
        {
            _avatarButton.onClick.AddListener(OnAvatarTapped);
        }

        public void SetGold(int amount)
        {
            if (_goldText != null) _goldText.text = amount.ToString("N0");
        }

        private void OnAvatarTapped()
        {
            Game.Core.UIManager.Instance?.ShowPopup<Game.OutGame.Settings.AccountPopupView>();
        }
    }
}
