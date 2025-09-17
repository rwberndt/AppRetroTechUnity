using System.Collections.Generic;
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
            le.preferredHeight = size.y > 0 ? size.y : 48f;
            le.minHeight = le.preferredHeight;
            le.flexibleHeight = 0f;

            var img = go.AddComponent<Image>();
            img.raycastTarget = true; // allow Button clicks on this card

            bool hasGradient = gradTop.HasValue && gradBottom.HasValue;

            ApplyRoundedCorners(go, img, radius);

            if (glass && !fill.HasValue)
            {
                img.color = new Color(1f, 1f, 1f, 0.08f);   // subtle white on purple bg
            }
            else if (hasGradient)
            {
                img.color = Color.white;
            }
            else
            {
                img.color = fill.HasValue ? (Color)fill.Value : Color.white;
            }

            if (hasGradient)
            {
                var gradient = go.GetComponent<UiVerticalGradient>();
                if (gradient == null)
                    gradient = go.AddComponent<UiVerticalGradient>();
                gradient.SetColors(gradTop.Value, gradBottom.Value);
            }
            else
            {
                var gradient = go.GetComponent<UiVerticalGradient>();
                if (gradient != null)
                {
                    Object.Destroy(gradient);
                }

                if (!glass || fill.HasValue)
                    img.color = fill.HasValue ? (Color)fill.Value : img.color;
            }

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
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.enableWordWrapping = true;
            return t;
        }

        private static readonly Dictionary<int, Sprite> RoundedSpriteCache = new();
        private static Sprite _roundedPanelSprite;
        private static bool _roundedPanelChecked;

        private static void ApplyRoundedCorners(GameObject go, Image img, float radius)
        {
            if (img == null)
                return;

            var mask = go.GetComponent<Mask>();

            if (radius <= 0f)
            {
                img.sprite = null;
                img.type = Image.Type.Simple;
                img.pixelsPerUnitMultiplier = 1f;

                if (mask != null)
                {
                    Object.Destroy(mask);
                }

                return;
            }

            var sprite = LoadRoundedSprite(radius);
            if (sprite != null)
            {
                img.sprite = sprite;
                img.type = Image.Type.Sliced;
                img.pixelsPerUnitMultiplier = sprite == _roundedPanelSprite
                    ? CalculatePixelsPerUnit(sprite, radius)
                    : 1f;

                if (mask == null)
                {
                    mask = go.AddComponent<Mask>();
                }

                mask.showMaskGraphic = true;
            }
            else
            {
                img.sprite = null;
                img.type = Image.Type.Simple;
                img.pixelsPerUnitMultiplier = 1f;

                if (mask != null)
                {
                    Object.Destroy(mask);
                }
            }
        }

        private static float CalculatePixelsPerUnit(Sprite sprite, float desiredRadius)
        {
            if (sprite == null)
                return 1f;

            var border = sprite.border;
            float referenceRadius = border.x;

            if (referenceRadius <= 0f || desiredRadius <= 0f)
                return 1f;

            return Mathf.Max(0.01f, referenceRadius / desiredRadius);
        }

        private static Sprite LoadRoundedSprite(float radius)
        {
            if (!_roundedPanelChecked)
            {
                _roundedPanelSprite = Resources.Load<Sprite>("Sprites/RoundedPanel");
                _roundedPanelChecked = true;
            }

            if (_roundedPanelSprite != null)
            {
                return _roundedPanelSprite;
            }

            return GetGeneratedRoundedSprite(radius);
        }

        private static Sprite GetGeneratedRoundedSprite(float radius)
        {
            if (radius <= 0f)
                return null;

            int key = Mathf.Max(1, Mathf.RoundToInt(radius * 100f));

            if (RoundedSpriteCache.TryGetValue(key, out var cached) && cached != null)
                return cached;

            var sprite = BuildRoundedSprite(radius);
            RoundedSpriteCache[key] = sprite;
            return sprite;
        }

        private static Sprite BuildRoundedSprite(float radius)
        {
            float clampedRadius = Mathf.Max(1f, radius);
            int textureRadius = Mathf.CeilToInt(clampedRadius);
            int size = Mathf.Max(2, textureRadius * 2);

            var tex = new Texture2D(size, size, TextureFormat.Alpha8, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            var colors = new Color32[size * size];
            var opaque = new Color32(255, 255, 255, 255);
            var transparent = new Color32(255, 255, 255, 0);

            float radiusSquared = clampedRadius * clampedRadius;
            float left = clampedRadius;
            float right = size - clampedRadius;
            float bottom = clampedRadius;
            float top = size - clampedRadius;

            for (int y = 0; y < size; y++)
            {
                float py = y + 0.5f;
                for (int x = 0; x < size; x++)
                {
                    float px = x + 0.5f;
                    bool inside = true;

                    if (px < left && py < bottom)
                    {
                        inside = IsInsideCorner(px, py, left, bottom, radiusSquared);
                    }
                    else if (px > right && py < bottom)
                    {
                        inside = IsInsideCorner(px, py, right, bottom, radiusSquared);
                    }
                    else if (px < left && py > top)
                    {
                        inside = IsInsideCorner(px, py, left, top, radiusSquared);
                    }
                    else if (px > right && py > top)
                    {
                        inside = IsInsideCorner(px, py, right, top, radiusSquared);
                    }

                    colors[y * size + x] = inside ? opaque : transparent;
                }
            }

            tex.SetPixels32(colors);
            tex.Apply();

            var sprite = Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 1f);
            sprite.name = $"RoundedRuntime_{radius:0.##}";
            sprite.border = new Vector4(clampedRadius, clampedRadius, clampedRadius, clampedRadius);
            return sprite;
        }

        private static bool IsInsideCorner(float px, float py, float cx, float cy, float radiusSquared)
        {
            float dx = px - cx;
            float dy = py - cy;
            return dx * dx + dy * dy <= radiusSquared;
        }
    }
}
