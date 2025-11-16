using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace RetroTech
{
    /// <summary>
    /// Tiny kit to create rounded gradient surfaces, glass cards and TMP text.
    /// Every method is safe if optional packages are missing.
    /// </summary>
    public static class UiKit
    {
        public static readonly Color32 TextMain = new(245, 245, 255, 255);
        public static readonly Color32 TextMuted = new(210, 210, 235, 255);
        public static readonly Color32 GlassBg = new(255, 255, 255, 30);
        public static readonly Color32 GlassLine = new(255, 255, 255, 60);

        public const int DefaultPageTitleFontSize = 56;
        private const float PageTitleHorizontalPadding = 24f;
        private const float PageTitleTopPadding = 32f;

        // Create a rounded panel. When gradTop/bottom are provided, paints a vertical gradient
        public static Image CreateCard(
        Transform parent,
        Vector2 size,              // use size.y as row height
        Color32? fill,
        float radius = 22f,
        bool glass = false,
        Color? gradTop = null,
        Color? gradBottom = null)
        {
            var go = new GameObject("Card");
            go.transform.SetParent(parent, false);

            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var le = go.AddComponent<LayoutElement>();
            float targetHeight = size.y > 0 ? ResponsiveTypography.ResponsiveSpacing(size.y) : 48f;
            le.preferredHeight = targetHeight;
            le.minHeight = le.preferredHeight;
            le.flexibleHeight = 0f;

            var img = go.AddComponent<Image>();
            img.raycastTarget = true; // allow Button clicks on this card

            if (glass && !fill.HasValue)
                img.color = new Color(1f, 1f, 1f, 0.08f);   // subtle white on purple bg
            else
                img.color = fill.HasValue ? (Color)fill.Value : Color.white;

            // (optional) if you have a 9-sliced rounded sprite, enable slicing:
            // img.type = Image.Type.Sliced;
            // img.sprite = YourRoundedSprite;

            return img;
        }

        public static TMP_Text TMP(Transform parent, string text, int size, Color32 color,
                                   TextAlignmentOptions align = TextAlignmentOptions.MidlineLeft,
                                   bool bold = false)
        {
            var go = new GameObject("TMP");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, size + 18);
            var t = go.AddComponent<TextMeshProUGUI>();
            t.text = text;
            ResponsiveTypography.ApplyToTMP(t, size);
            t.color = color;
            t.alignment = align;
            t.enableWordWrapping = true;
            t.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
            return t;
        }

        public static (GameObject container, TextMeshProUGUI label) CreatePageTitle(
            Transform parent,
            string text,
            int fontSize = DefaultPageTitleFontSize,
            Color32? color = null,
            TextAlignmentOptions alignment = TextAlignmentOptions.Left)
        {
            var container = new GameObject("PageTitle", typeof(RectTransform), typeof(LayoutElement));
            container.transform.SetParent(parent, false);

            var layout = container.GetComponent<LayoutElement>();
            float height = ResponsiveTypography.ResponsiveSpacing(fontSize + 32f + PageTitleTopPadding);
            layout.minHeight = height;
            layout.preferredHeight = height;
            layout.flexibleHeight = 0f;

            var label = TMP(container.transform, text, fontSize, color ?? TextMain, alignment, bold: true) as TextMeshProUGUI;
            if (label != null)
            {
                var labelRT = label.rectTransform;
                labelRT.anchorMin = Vector2.zero;
                labelRT.anchorMax = Vector2.one;
                labelRT.offsetMin = new Vector2(PageTitleHorizontalPadding, 0f);
                labelRT.offsetMax = new Vector2(-PageTitleHorizontalPadding, -PageTitleTopPadding);
                label.margin = Vector4.zero;
            }

            return (container, label);
        }
    }
}
