using TMPro;
using UnityEngine;

namespace RetroTech
{
    /// <summary>
    /// Utilitário centralizado para deixar a tipografia e os espaçamentos mais responsivos
    /// em diferentes tamanhos e densidades de tela.
    /// </summary>
    public static class ResponsiveTypography
    {
        private const float ReferenceMinDimension = 1080f;
        private const float ReferenceDpi = 160f;
        private const float AccessibilityBoost = 1.12f;

        private static int _cachedFrame = -1;
        private static float _cachedScale = -1f;

        private static float GetFontScale()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                return CalculateFontScale();
#endif
            if (_cachedFrame == Time.frameCount && _cachedScale > 0f)
                return _cachedScale;

            _cachedScale = CalculateFontScale();
            _cachedFrame = Time.frameCount;
            return _cachedScale;
        }

        /// <summary>
        /// Calcula um fator de escala suavizado considerando tamanho da tela e densidade.
        /// O valor mínimo é 1 para evitar textos minúsculos e o máximo limita exageros.
        /// </summary>
        private static float CalculateFontScale()
        {
            var safeArea = Screen.safeArea;
            float minDimension = Mathf.Min(safeArea.width, safeArea.height);
            if (minDimension <= 0f)
                minDimension = Mathf.Min(Screen.width, Screen.height);
            if (minDimension <= 0f)
                minDimension = ReferenceMinDimension;

            float sizeScale = minDimension / ReferenceMinDimension;

            float dpi = Screen.dpi;
            float dpiScale = dpi <= 0f ? 1f : Mathf.Clamp(dpi / ReferenceDpi, 0.9f, 1.6f);

            // Combina ambos os fatores de forma suave para evitar saltos bruscos
            float combined = Mathf.Sqrt(sizeScale * dpiScale);

            // Interfaces em desktops normalmente precisam de um reforço extra
            if (SystemInfo.deviceType == DeviceType.Desktop)
                combined = Mathf.Max(combined, 1.15f);

            return Mathf.Clamp(combined, 1f, 1.75f);
        }

        /// <summary>
        /// Retorna o tamanho de fonte ajustado a partir de um tamanho base.
        /// </summary>
        public static int ResponsiveFontSize(int baseSize)
        {
            if (baseSize <= 0)
                return baseSize;

            float scale = GetFontScale() * AccessibilityBoost;
            return Mathf.RoundToInt(baseSize * scale);
        }

        /// <summary>
        /// Ajusta espaçamentos verticais mantendo proporções agradáveis.
        /// </summary>
        public static float ResponsiveSpacing(float baseSpacing)
        {
            if (baseSpacing <= 0f)
                return baseSpacing;

            float scale = GetFontScale();
            float spacingScale = Mathf.Lerp(1f, scale, 0.6f);
            return baseSpacing * Mathf.Clamp(spacingScale, 1f, 1.5f);
        }

        /// <summary>
        /// Aplica tamanho de fonte responsivo diretamente em um TextMeshProUGUI.
        /// </summary>
        public static void ApplyToTMP(TextMeshProUGUI tmp, int baseSize, bool allowShrink = true)
        {
            if (tmp == null)
                return;

            int responsiveSize = ResponsiveFontSize(baseSize);
            tmp.fontSize = responsiveSize;
            tmp.enableAutoSizing = allowShrink;

            if (allowShrink)
            {
                tmp.fontSizeMax = responsiveSize;
                tmp.fontSizeMin = Mathf.Max(20f, responsiveSize * 0.7f);
            }
        }
    }
}
