using UnityEngine;

namespace RetroTech
{
    /// <summary>
    /// Provides helpers to scale UI metrics based on the current screen size so the
    /// interface stays legible across phones with very different resolutions.
    /// </summary>
    public static class ResponsiveMetrics
    {
        private const float MinReferenceMinDimension = 720f;
        private const float MaxReferenceMinDimension = 1440f;

        private static float GetNormalizedMinDimension()
        {
            if (Screen.width <= 0f || Screen.height <= 0f)
                return 0.5f;

            float minDimension = Mathf.Min(Screen.width, Screen.height);
            float clamped = Mathf.Clamp(minDimension, MinReferenceMinDimension, MaxReferenceMinDimension);
            return Mathf.InverseLerp(MinReferenceMinDimension, MaxReferenceMinDimension, clamped);
        }

        /// <summary>
        /// Returns a scale that can be applied to font sizes. Smaller screens receive a
        /// slightly larger scale to improve readability.
        /// </summary>
        public static float GetFontScale(float smallScreenScale = 1.15f, float largeScreenScale = 1.0f)
        {
            float normalized = GetNormalizedMinDimension();
            return Mathf.Lerp(smallScreenScale, largeScreenScale, normalized);
        }

        /// <summary>
        /// Returns a scale that can be used to slightly compress layout spacing on smaller
        /// screens while keeping more generous spacing on larger devices.
        /// </summary>
        public static float GetSpacingScale(float smallScreenScale = 0.9f, float largeScreenScale = 1.0f)
        {
            float normalized = GetNormalizedMinDimension();
            return Mathf.Lerp(smallScreenScale, largeScreenScale, normalized);
        }

        /// <summary>
        /// Helper to interpolate between two values based on the current screen size.
        /// </summary>
        public static float LerpByDevice(float smallScreenValue, float largeScreenValue)
        {
            float normalized = GetNormalizedMinDimension();
            return Mathf.Lerp(smallScreenValue, largeScreenValue, normalized);
        }

        /// <summary>
        /// Returns a reference resolution that increases the perceived size of the UI on
        /// small screens and prevents oversized elements on very large displays.
        /// </summary>
        public static Vector2 GetReferenceResolution(
            Vector2 baseReferenceResolution,
            float smallScreenScale = 1.2f,
            float largeScreenScale = 0.95f)
        {
            float normalized = GetNormalizedMinDimension();
            float scale = Mathf.Lerp(smallScreenScale, largeScreenScale, normalized);
            if (scale <= 0f)
                scale = 1f;

            return baseReferenceResolution / scale;
        }

        /// <summary>
        /// Provides a match value for CanvasScaler so the UI adapts gracefully to different
        /// aspect ratios.
        /// </summary>
        public static float GetCanvasMatch()
        {
            if (Screen.width <= 0f || Screen.height <= 0f)
                return 0.5f;

            float width = Screen.width;
            float height = Screen.height;

            if (height >= width)
            {
                float aspect = width / height;
                float normalized = Mathf.InverseLerp(0.45f, 0.75f, Mathf.Clamp(aspect, 0.45f, 0.75f));
                return Mathf.Lerp(0.58f, 0.42f, normalized);
            }
            else
            {
                float aspect = width / height;
                float normalized = Mathf.InverseLerp(1f, 2f, Mathf.Clamp(aspect, 1f, 2f));
                return Mathf.Lerp(0.42f, 0.25f, normalized);
            }
        }
    }
}
