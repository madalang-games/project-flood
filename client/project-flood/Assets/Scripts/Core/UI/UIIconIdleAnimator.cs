using UnityEngine;

namespace Game.Core.UI
{
    /// <summary>
    /// Lightweight, C#-based icon idle animator.
    /// Modifies Transform properties (scale, position, rotation) purely in code,
    /// avoiding Unity Animator/Sprite locks and rendering conflicts on target devices.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class UIIconIdleAnimator : MonoBehaviour
    {
        public enum AnimationType
        {
            Pulse,
            Float,
            Rotate
        }

        [SerializeField] private AnimationType _animationType = AnimationType.Float;
        [SerializeField] private float _speed = 2f;
        [SerializeField] private float _amount = 10f; // Scale percent (e.g. 5 for 5%) for Pulse, pixels for Float, degrees for Rotate

        private RectTransform _rt;
        private Vector3 _initialScale;
        private Vector2 _initialPosition;
        private float _timeOffset;

        private void Awake()
        {
            _rt = GetComponent<RectTransform>();
            _initialScale = _rt.localScale;
            _initialPosition = _rt.anchoredPosition;
            
            // Randomize phase offset to ensure multiple instances on screen breathe asynchronously
            _timeOffset = Random.Range(0f, 100f);
        }

        private void OnEnable()
        {
            // Reset to clean initial states on enable
            if (_rt != null)
            {
                _rt.localScale = _initialScale;
                _rt.anchoredPosition = _initialPosition;
            }
        }

        private void Update()
        {
            if (_rt == null) return;

            float t = (Time.time + _timeOffset) * _speed;

            switch (_animationType)
            {
                case AnimationType.Float:
                    float newY = _initialPosition.y + Mathf.Sin(t) * _amount;
                    _rt.anchoredPosition = new Vector2(_initialPosition.x, newY);
                    break;

                case AnimationType.Pulse:
                    float s = 1f + Mathf.Sin(t) * (_amount * 0.01f);
                    _rt.localScale = _initialScale * s;
                    break;

                case AnimationType.Rotate:
                    float r = Mathf.Sin(t) * _amount;
                    _rt.localRotation = Quaternion.Euler(0f, 0f, r);
                    break;
            }
        }

        // Helper setters for configuring animation programmatically if needed
        public void Configure(AnimationType type, float speed, float amount)
        {
            _animationType = type;
            _speed = speed;
            _amount = amount;
        }
    }
}
