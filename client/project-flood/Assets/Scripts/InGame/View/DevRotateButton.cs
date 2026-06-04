#if UNITY_EDITOR || DEVELOPMENT_BUILD
using Game.InGame.Controller;
using UnityEngine;
using UnityEngine.UI;

namespace Game.InGame.View
{
    // Dev-only button that triggers 180-degree board rotation.
    // If this component is attached to a UI Button, it wires that Button.
    // If not, it creates a small floating fallback button.
    public class DevRotateButton : MonoBehaviour
    {
        [SerializeField] private InGameController _controller;

        private Button _button;

        private void Start()
        {
            if (_controller == null)
                _controller = FindAnyObjectByType<InGameController>();

            _button = GetComponent<Button>();
            if (_button != null)
                WireButton(_button);
            else
                CreateButton();
        }

        private void OnDestroy()
        {
            if (_button != null)
                _button.onClick.RemoveListener(OnClick);
        }

        private void CreateButton()
        {
            var canvasGo = new GameObject("DevRotateCanvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            canvasGo.AddComponent<CanvasScaler>();
            canvasGo.AddComponent<GraphicRaycaster>();

            var btnGo = new GameObject("DevRotateBtn");
            btnGo.transform.SetParent(canvasGo.transform, false);

            var img = btnGo.AddComponent<Image>();
            img.color = new Color(0.27f, 0.27f, 0.55f, 0.85f);

            var btn = btnGo.AddComponent<Button>();
            btn.targetGraphic = img;
            var colors = btn.colors;
            colors.highlightedColor = new Color(0.35f, 0.35f, 0.70f, 0.95f);
            colors.pressedColor = new Color(0.20f, 0.20f, 0.45f, 1f);
            btn.colors = colors;
            WireButton(btn);

            var rt = btnGo.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(1f, 0f);
            rt.anchoredPosition = new Vector2(-16f, 16f);
            rt.sizeDelta = new Vector2(112f, 40f);

            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(btnGo.transform, false);
            var txt = labelGo.AddComponent<Text>();
            txt.text = "Rotate 180";
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = 14;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;

            var lrt = labelGo.GetComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = lrt.offsetMax = Vector2.zero;
        }

        private void WireButton(Button button)
        {
            _button = button;
            _button.onClick.RemoveListener(OnClick);
            _button.onClick.AddListener(OnClick);
        }

        private void OnClick()
        {
            _controller?.TriggerRotateBoard();
        }
    }
}
#endif
