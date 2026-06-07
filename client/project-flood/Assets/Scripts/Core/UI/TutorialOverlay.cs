using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Game.Services.Tutorial;
using ProjectFlood.Contracts.GameTypes;
using ProjectFlood.Data.Generated;
using Game.InGame.View;
using Game.InGame.Controller;

namespace Game.Core.UI
{
    public class TutorialOverlay : MonoBehaviour
    {
        [SerializeField] private RectTransform _spotlightCutout;
        [SerializeField] private Image _spotlightGlow; // optional glow border
        [SerializeField] private RectTransform _tooltipBubble;
        [SerializeField] private TMP_Text _tooltipText;
        [SerializeField] private RectTransform _fingerOverlay;
        [SerializeField] private Image _characterAvatar; // Guide character visual (Floodie)
        [SerializeField] private Button _fullscreenDismissButton; // for non-blocking taps

        private TutorialStepSequencer _sequencer;
        private Coroutine _autoAdvanceCoroutine;
        private Coroutine _pulseCoroutine;
        private Transform _currentTargetTransform;
        private RectTransform _currentTargetRT;
        private TargetSpaceType _currentTargetSpace;
        private Vector2 _designedResolution = new Vector2(1080, 1920);

        public void Init(TutorialStepSequencer sequencer)
        {
            _sequencer = sequencer;
            _sequencer.OnStepChanged += ShowStep;
            _sequencer.OnComplete += Close;

            if (_fullscreenDismissButton != null)
            {
                _fullscreenDismissButton.onClick.AddListener(OnFullscreenTapped);
            }

            if (_spotlightGlow != null)
            {
                _pulseCoroutine = StartCoroutine(AnimateGlowPulse());
            }

            ShowStep(_sequencer.CurrentStep);
        }

        private void OnDestroy()
        {
            if (_sequencer != null)
            {
                _sequencer.OnStepChanged -= ShowStep;
                _sequencer.OnComplete -= Close;
            }

            if (_pulseCoroutine != null) StopCoroutine(_pulseCoroutine);
            if (_autoAdvanceCoroutine != null) StopCoroutine(_autoAdvanceCoroutine);
        }

        private void LateUpdate()
        {
            // Responsive update in case of layout shifts or board animations
            UpdateSpotlightPosition();
        }

        private void ShowStep(TutorialStep step)
        {
            if (step == null) return;

            if (_autoAdvanceCoroutine != null)
            {
                StopCoroutine(_autoAdvanceCoroutine);
                _autoAdvanceCoroutine = null;
            }

            // Update localized text
            if (_tooltipText != null)
            {
                _tooltipText.text = Services.LocalizationService.Instance != null 
                    ? Services.LocalizationService.Instance.Get(step.text_key) 
                    : step.text_key;
            }

            // Locate target
            ResolveTarget(step);

            // Enable/disable fullscreen advance trigger based on is_blocking
            if (_fullscreenDismissButton != null)
            {
                // Fullscreen advance is enabled ONLY if it's not a blocking step
                _fullscreenDismissButton.gameObject.SetActive(!step.is_blocking);
            }

            // Trigger bubble anim (Scale up)
            if (_tooltipBubble != null)
            {
                _tooltipBubble.localScale = Vector3.zero;
                StartCoroutine(AnimateBubbleAppear());
            }

            // Set up auto-advance timer if specified
            if (step.auto_advance_sec > 0.01f)
            {
                _autoAdvanceCoroutine = StartCoroutine(AutoAdvanceTimer(step.auto_advance_sec));
            }

            // Instantly evaluate position for this frame
            UpdateSpotlightPosition();
        }

        private void ResolveTarget(TutorialStep step)
        {
            _currentTargetTransform = null;
            _currentTargetRT = null;
            _currentTargetSpace = step.target_space;

            if (string.IsNullOrEmpty(step.target_ui_id))
            {
                _spotlightCutout.gameObject.SetActive(false);
                _fingerOverlay.gameObject.SetActive(false);
                return;
            }

            _spotlightCutout.gameObject.SetActive(true);
            _fingerOverlay.gameObject.SetActive(step.content_type == TutorialContentType.FingerOverlay);

            if (step.target_space == TargetSpaceType.World)
            {
                var boardView = FindObjectOfType<BoardView>();
                if (boardView != null)
                {
                    if (step.target_ui_id.StartsWith("board_cell_"))
                    {
                        if (ParseCellTarget(step.target_ui_id, out int r, out int c))
                        {
                            var cellView = boardView.GetCellView(r, c);
                            if (cellView != null) _currentTargetTransform = cellView.transform;
                        }
                    }
                    else if (step.target_ui_id == "board_protector_cell")
                    {
                        _currentTargetTransform = FindCellWithProtector(boardView);
                    }
                    else if (step.target_ui_id == "board_core_cell")
                    {
                        _currentTargetTransform = FindCellWithCore(boardView);
                    }
                    else if (step.target_ui_id == "board_obstacle_cell")
                    {
                        _currentTargetTransform = FindCellWithObstacle(boardView);
                    }
                    else if (step.target_ui_id == "board_area")
                    {
                        _currentTargetTransform = boardView.transform;
                    }
                }
            }
            else
            {
                // UI Space Target
                var go = GameObject.Find(step.target_ui_id);
                if (go != null)
                {
                    _currentTargetRT = go.GetComponent<RectTransform>();
                }
                else
                {
                    // Find in Resources / active hierarchy if not instantly resolved
                    var allRts = Resources.FindObjectsOfTypeAll<RectTransform>();
                    foreach (var rt in allRts)
                    {
                        if (rt.gameObject.activeInHierarchy && rt.gameObject.name == step.target_ui_id)
                        {
                            _currentTargetRT = rt;
                            break;
                        }
                    }
                }
            }
        }

        private Transform FindCellWithProtector(BoardView boardView)
        {
            for (int r = 0; r < 20; r++)
            for (int c = 0; c < 20; c++)
            {
                var cellView = boardView.GetCellView(r, c);
                if (cellView != null && cellView.gameObject.activeSelf)
                {
                    var renderers = cellView.GetComponentsInChildren<SpriteRenderer>(false);
                    foreach (var rdr in renderers)
                    {
                        if (rdr.gameObject.name.Contains("Protector") && rdr.gameObject.activeSelf)
                            return cellView.transform;
                    }
                }
            }
            return null;
        }

        private Transform FindCellWithCore(BoardView boardView)
        {
            for (int r = 0; r < 20; r++)
            for (int c = 0; c < 20; c++)
            {
                var cellView = boardView.GetCellView(r, c);
                if (cellView != null && cellView.gameObject.activeSelf)
                {
                    foreach (Transform child in cellView.transform)
                    {
                        if (child.gameObject.name.Contains("Core") && child.gameObject.activeSelf)
                            return cellView.transform;
                    }
                }
            }
            return null;
        }

        private Transform FindCellWithObstacle(BoardView boardView)
        {
            for (int r = 0; r < 20; r++)
            for (int c = 0; c < 20; c++)
            {
                var cellView = boardView.GetCellView(r, c);
                if (cellView != null && cellView.gameObject.activeSelf)
                {
                    var srs = cellView.GetComponentsInChildren<SpriteRenderer>();
                    foreach (var s in srs)
                    {
                        if (s.sprite != null && s.sprite.name.Contains("obstacle"))
                            return cellView.transform;
                    }
                }
            }
            return null;
        }

        private void UpdateSpotlightPosition()
        {
            if (_sequencer == null || _sequencer.CurrentStep == null) return;

            var step = _sequencer.CurrentStep;
            Canvas overlayCanvas = UIManager.Instance?.GetComponentInChildren<Canvas>();
            if (overlayCanvas == null) overlayCanvas = GetComponentInParent<Canvas>();
            if (overlayCanvas == null) return;

            RectTransform overlayRt = overlayCanvas.GetComponent<RectTransform>();
            Vector2 screenPos = Vector2.zero;
            Vector2 targetSize = new Vector2(150, 150); // fallback size

            if (_currentTargetSpace == TargetSpaceType.World && _currentTargetTransform != null)
            {
                var cellView = _currentTargetTransform.GetComponent<CellView>();
                Vector3 worldCenter = _currentTargetTransform.position;
                if (cellView != null) worldCenter = cellView.GetWorldCenter();

                screenPos = Camera.main.WorldToScreenPoint(worldCenter);

                if (cellView != null)
                {
                    float width = cellView.GetScreenBounds().size.x;
                    // Project world size to screen space bounds
                    Vector3 screenEdge = Camera.main.WorldToScreenPoint(worldCenter + new Vector3(width * 0.5f, 0, 0));
                    float r = Mathf.Abs(screenEdge.x - screenPos.x) * 2f;
                    targetSize = new Vector2(r, r) * 1.15f; // Add safe margin
                }
                else
                {
                    // Generic Board Area Spotlight
                    var boardView = _currentTargetTransform.GetComponent<BoardView>();
                    if (boardView != null)
                    {
                        // Spotlight entire board
                        targetSize = new Vector2(Screen.width * 0.95f, Screen.width * 0.95f);
                    }
                }
            }
            else if (_currentTargetSpace == TargetSpaceType.UI && _currentTargetRT != null)
            {
                screenPos = RectTransformUtility.WorldToScreenPoint(null, _currentTargetRT.position);
                targetSize = _currentTargetRT.rect.size * _currentTargetRT.lossyScale / overlayCanvas.scaleFactor;
            }
            else
            {
                // No target - hide or spotlight middle
                _spotlightCutout.gameObject.SetActive(false);
                _fingerOverlay.gameObject.SetActive(false);
                return;
            }

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                overlayRt,
                screenPos,
                overlayCanvas.worldCamera,
                out Vector2 localPoint
            );

            _spotlightCutout.anchoredPosition = localPoint;
            _spotlightCutout.sizeDelta = targetSize + new Vector2(20f, 20f); // margins

            // Position finger indicator relative to target center
            if (_fingerOverlay.gameObject.activeSelf)
            {
                _fingerOverlay.anchoredPosition = localPoint + new Vector2(40f, -40f);
            }

            // Position tooltip bubble nearby target
            if (_tooltipBubble != null)
            {
                // Place bubble above target if target is low, below target if target is high
                float halfHeight = overlayRt.rect.height * 0.5f;
                float verticalOffset = localPoint.y > 0 ? -280f : 280f;
                _tooltipBubble.anchoredPosition = new Vector2(0f, Mathf.Clamp(localPoint.y + verticalOffset, -halfHeight + 200f, halfHeight - 200f));
            }
        }

        private void OnFullscreenTapped()
        {
            if (_sequencer != null && _sequencer.IsActive)
            {
                var step = _sequencer.CurrentStep;
                if (step != null && !step.is_blocking)
                {
                    _sequencer.Next();
                }
            }
        }

        private IEnumerator AutoAdvanceTimer(float seconds)
        {
            yield return new WaitForSeconds(seconds);
            if (_sequencer != null && _sequencer.IsActive)
            {
                _sequencer.Next();
            }
        }

        private IEnumerator AnimateBubbleAppear()
        {
            float duration = 0.2f;
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                float p = t / duration;
                float scale = EaseOutBack(p);
                _tooltipBubble.localScale = new Vector3(scale, scale, 1f);
                yield return null;
            }
            _tooltipBubble.localScale = Vector3.one;
        }

        private IEnumerator AnimateGlowPulse()
        {
            while (true)
            {
                float t = Time.time * 4f;
                float alpha = 0.5f + 0.5f * Mathf.Sin(t);
                if (_spotlightGlow != null)
                {
                    Color color = _spotlightGlow.color;
                    color.a = alpha;
                    _spotlightGlow.color = color;
                }
                yield return null;
            }
        }

        private static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }

        private static bool ParseCellTarget(string targetId, out int row, out int col)
        {
            row = -1;
            col = -1;
            try
            {
                int rStart = targetId.IndexOf('[');
                int rEnd = targetId.IndexOf(']');
                int cStart = targetId.IndexOf('[', rEnd + 1);
                int cEnd = targetId.IndexOf(']', cStart + 1);
                
                if (rStart >= 0 && rEnd > rStart && cStart > rEnd && cEnd > cStart)
                {
                    string rStr = targetId.Substring(rStart + 1, rEnd - rStart - 1);
                    string cStr = targetId.Substring(cStart + 1, cEnd - cStart - 1);
                    if (int.TryParse(rStr, out row) && int.TryParse(cStr, out col))
                    {
                        return true;
                    }
                }
            }
            catch {}
            return false;
        }

        private void Close()
        {
            Destroy(gameObject);
        }
    }
}
