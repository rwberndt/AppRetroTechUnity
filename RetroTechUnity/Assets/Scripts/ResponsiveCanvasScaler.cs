using UnityEngine;
using UnityEngine.UI;

namespace RetroTech
{
    /// <summary>
    /// Keeps the CanvasScaler responsive by updating its reference resolution and
    /// width/height match whenever the screen size changes.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(CanvasScaler))]
    public class ResponsiveCanvasScaler : MonoBehaviour
    {
        [SerializeField]
        private Vector2 _baseReferenceResolution = new(1080f, 1920f);

        [SerializeField, Min(0.01f)]
        private float _smallScreenScale = 1.2f;

        [SerializeField, Min(0.01f)]
        private float _largeScreenScale = 0.95f;

        [SerializeField]
        private bool _applyMatchWidthOrHeight = true;

        private CanvasScaler _scaler;
        private Vector2 _lastResolution;
        private float _lastMatch;
        private int _lastScreenWidth;
        private int _lastScreenHeight;

        public Vector2 BaseReferenceResolution
        {
            get => _baseReferenceResolution;
            set
            {
                _baseReferenceResolution = value;
                Apply(true);
            }
        }

        public float SmallScreenScale
        {
            get => _smallScreenScale;
            set
            {
                _smallScreenScale = Mathf.Max(0.01f, value);
                Apply(true);
            }
        }

        public float LargeScreenScale
        {
            get => _largeScreenScale;
            set
            {
                _largeScreenScale = Mathf.Max(0.01f, value);
                Apply(true);
            }
        }

        public bool ApplyMatchWidthOrHeight
        {
            get => _applyMatchWidthOrHeight;
            set
            {
                _applyMatchWidthOrHeight = value;
                Apply(true);
            }
        }

        private void Awake()
        {
            _scaler = GetComponent<CanvasScaler>();
            EnsureScalerSetup();
        }

        private void OnEnable()
        {
            Apply(true);
        }

        private void Update()
        {
            if (_lastScreenWidth != Screen.width || _lastScreenHeight != Screen.height)
            {
                Apply(true);
            }
            else
            {
                Apply();
            }
        }

        private void EnsureScalerSetup()
        {
            if (_scaler == null)
                return;

            if (_scaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize)
                _scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

            if (_scaler.screenMatchMode != CanvasScaler.ScreenMatchMode.MatchWidthOrHeight)
                _scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        }

        private void Apply(bool force = false)
        {
            if (!isActiveAndEnabled)
                return;

            if (_scaler == null)
            {
                _scaler = GetComponent<CanvasScaler>();
                EnsureScalerSetup();
            }

            Vector2 targetResolution = ResponsiveMetrics.GetReferenceResolution(
                _baseReferenceResolution,
                _smallScreenScale,
                _largeScreenScale);

            if (force || Vector2.Distance(targetResolution, _lastResolution) > 0.1f)
            {
                _scaler.referenceResolution = targetResolution;
                _lastResolution = targetResolution;
            }

            if (_applyMatchWidthOrHeight)
            {
                float targetMatch = ResponsiveMetrics.GetCanvasMatch();
                if (force || !Mathf.Approximately(targetMatch, _lastMatch))
                {
                    _scaler.matchWidthOrHeight = targetMatch;
                    _lastMatch = targetMatch;
                }
            }

            _lastScreenWidth = Screen.width;
            _lastScreenHeight = Screen.height;
        }
    }
}
