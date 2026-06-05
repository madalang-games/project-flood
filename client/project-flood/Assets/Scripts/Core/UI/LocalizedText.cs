using Game.Services;
using TMPro;
using UnityEngine;

namespace Game.Core.UI
{
    /// <summary>
    /// Attach to any TMP_Text to enable localization.
    /// - stringId set   → updates text + font when language changes (static text)
    /// - stringId empty → updates font only (dynamic text; caller sets .text at runtime)
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public class LocalizedText : MonoBehaviour
    {
        [SerializeField] private string _stringId;

        private TMP_Text _tmp;

        void Awake()
        {
            _tmp = GetComponent<TMP_Text>();
            Apply();
        }

        void OnEnable()
        {
            if (LocalizationService.Instance != null)
                LocalizationService.Instance.OnLanguageChanged += Apply;
        }

        void OnDisable()
        {
            if (LocalizationService.Instance != null)
                LocalizationService.Instance.OnLanguageChanged -= Apply;
        }

        private void Apply()
        {
            if (LocalizationService.Instance == null) return;

            if (!string.IsNullOrEmpty(_stringId))
                _tmp.text = LocalizationService.Instance.Get(_stringId);

            var font = LocalizationService.Instance.GetFont(LocalizationService.Instance.CurrentLanguage);
            if (font != null)
                _tmp.font = font;
        }
    }
}
