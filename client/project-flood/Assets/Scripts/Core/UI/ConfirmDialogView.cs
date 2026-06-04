using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Core.UI
{
    public class ConfirmDialogView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _bodyText;
        [SerializeField] private TMP_Text _cancelLabel;
        [SerializeField] private TMP_Text _confirmLabel;
        [SerializeField] private Button   _cancelButton;
        [SerializeField] private Button   _confirmButton;
        [SerializeField] private Button   _backdropButton;
        [SerializeField] private Image    _confirmButtonImage;

        private Action _onConfirm;
        private Action _onCancel;

        private static readonly Color DangerColor = new Color(0.91f, 0.25f, 0.25f);

        public void Init(string title, string body, string confirmLabel,
                         Action onConfirm, Action onCancel = null,
                         string cancelLabel = "Cancel", bool danger = false)
        {
            _titleText.text   = title;
            _bodyText.text    = body ?? string.Empty;
            _bodyText.gameObject.SetActive(!string.IsNullOrEmpty(body));
            _cancelLabel.text  = cancelLabel;
            _confirmLabel.text = confirmLabel;

            if (danger && _confirmButtonImage != null)
                _confirmButtonImage.color = DangerColor;

            _onConfirm = onConfirm;
            _onCancel  = onCancel;

            _confirmButton.onClick.AddListener(OnConfirm);
            _cancelButton.onClick.AddListener(OnCancel);
            if (_backdropButton != null) _backdropButton.onClick.AddListener(OnCancel);
        }

        private void OnConfirm()
        {
            _onConfirm?.Invoke();
            Close();
        }

        private void OnCancel()
        {
            _onCancel?.Invoke();
            Close();
        }

        private void Close()
        {
            var appear = GetComponent<UIPanelAppear>();
            if (appear != null)
                appear.Disappear(() => Game.Core.UIManager.Instance?.CloseTopPopup());
            else
                Game.Core.UIManager.Instance?.CloseTopPopup();
        }
    }
}
