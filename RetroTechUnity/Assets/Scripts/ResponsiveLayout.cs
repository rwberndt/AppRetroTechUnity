using UnityEngine;

namespace RetroTech
{
    /// <summary>
    /// Utilitário auxiliar para ajustar paddings, espaçamentos e margens
    /// de acordo com o tamanho da tela. Ele complementa o <see cref="ResponsiveTypography"/>
    /// oferecendo medidas proporcionais para os elementos de layout.
    /// </summary>
    public static class ResponsiveLayout
    {
        private const float SmallWidthThreshold = 420f;
        private const float MediumWidthThreshold = 600f;
        private const float LargeWidthThreshold = 840f;

        private static float GetSafeWidth()
        {
            var safeArea = Screen.safeArea;
            float width = safeArea.width;

            if (width <= 0f)
            {
                width = Mathf.Min(Screen.width, Screen.height);
            }

            if (width <= 0f)
            {
                width = LargeWidthThreshold;
            }

            return width;
        }

        /// <summary>
        /// Calcula o padding horizontal recomendado para o conteúdo baseado na largura disponível.
        /// </summary>
        public static int CalculateHorizontalPadding(int basePadding)
        {
            if (basePadding <= 0)
            {
                return basePadding;
            }

            float width = GetSafeWidth();
            float factor;

            if (width <= SmallWidthThreshold)
            {
                factor = 0.55f;
            }
            else if (width <= MediumWidthThreshold)
            {
                factor = 0.75f;
            }
            else if (width <= LargeWidthThreshold)
            {
                factor = 0.9f;
            }
            else
            {
                factor = 1f;
            }

            return Mathf.Max(8, Mathf.RoundToInt(basePadding * factor));
        }

        /// <summary>
        /// Ajusta um padding vertical seguindo o mesmo padrão responsivo dos espaçamentos.
        /// </summary>
        public static int CalculateVerticalPadding(int basePadding)
        {
            if (basePadding <= 0)
            {
                return basePadding;
            }

            float spacing = ResponsiveTypography.ResponsiveSpacing(basePadding);
            return Mathf.Max(8, Mathf.RoundToInt(spacing));
        }

        /// <summary>
        /// Retorna um espaçamento suavemente escalonado a partir de um valor base.
        /// </summary>
        public static float CalculateSpacing(float baseSpacing)
        {
            if (baseSpacing <= 0f)
            {
                return baseSpacing;
            }

            float spacing = ResponsiveTypography.ResponsiveSpacing(baseSpacing);
            float min = baseSpacing * 0.6f;
            float max = baseSpacing * 1.5f;
            return Mathf.Clamp(spacing, min, max);
        }

        /// <summary>
        /// Calcula o padding interno ideal para cartões e containers.
        /// </summary>
        public static float CalculateCardPadding(float basePadding)
        {
            float width = GetSafeWidth();
            float factor;

            if (width <= SmallWidthThreshold)
            {
                factor = 0.65f;
            }
            else if (width <= MediumWidthThreshold)
            {
                factor = 0.85f;
            }
            else
            {
                factor = 1f;
            }

            return Mathf.Max(12f, basePadding * factor);
        }
    }
}
