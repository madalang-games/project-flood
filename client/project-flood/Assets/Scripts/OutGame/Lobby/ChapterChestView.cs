using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Game.OutGame.Lobby
{
    public class ChapterChestView : MonoBehaviour
    {
        public enum ChestState
        {
            Inactive,
            Active,
            Claimed
        }

        [SerializeField] private Image _chestImage;
        [SerializeField] private TMP_Text _statusText;
        [SerializeField] private Button _button;
        
        [Header("Visual States")]
        [SerializeField] private Sprite _inactiveSprite;
        [SerializeField] private Sprite _activeSprite;
        [SerializeField] private Sprite _claimedSprite;

        [Header("Effects")]
        [SerializeField] private GameObject _glowEffect; // Activated when ChestState.Active
        [SerializeField] private CanvasGroup _canvasGroup; // Dims chest when Inactive

        private ChestState _state;
        public ChestState State => _state;

        public void SetState(ChestState state)
        {
            _state = state;
            
            if (_chestImage != null)
            {
                _chestImage.sprite = state switch
                {
                    ChestState.Inactive => _inactiveSprite,
                    ChestState.Active => _activeSprite,
                    ChestState.Claimed => _claimedSprite,
                    _ => _inactiveSprite
                };
            }

            if (_statusText != null)
            {
                _statusText.text = state switch
                {
                    ChestState.Inactive => "Locked",
                    ChestState.Active => "Claim!",
                    ChestState.Claimed => "Cleared",
                    _ => ""
                };
            }

            if (_button != null)
            {
                // Always interactable to show information toast or claim reward.
                // It is disabled only when already Claimed.
                _button.interactable = (state != ChestState.Claimed);
            }

            if (_glowEffect != null)
            {
                _glowEffect.SetActive(state == ChestState.Active);
            }

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = (state == ChestState.Inactive) ? 0.6f : 1.0f;
            }
        }
    }
}
