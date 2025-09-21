using System;
using System.Collections.Generic;
using System.Reflection;
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

        private static readonly Dictionary<string, Sprite> GradientSpriteCache = new();
        private static readonly List<Texture2D> GeneratedTextures = new();
        private static readonly BindingFlags MemberFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

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

            if (glass && !fill.HasValue)
                img.color = new Color(1f, 1f, 1f, 0.08f);   // subtle white on purple bg
            else
                img.color = fill.HasValue ? (Color)fill.Value : Color.white;

            if (gradTop.HasValue && gradBottom.HasValue)
            {
                ApplyVerticalGradient(img, gradTop.Value, gradBottom.Value);
            }

            TryApplyRoundedCorners(go, radius);

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
            t.raycastTarget = false;

            if (bold)
            {
                t.fontStyle |= FontStyles.Bold;
            }

            return t;
        }

        public static void ApplyVerticalGradient(Image target, Color top, Color bottom)
        {
            if (!target) return;

            if (TryApplyMobSakaiGradient(target, top, bottom))
            {
                target.color = Color.white;
                target.SetAllDirty();
                return;
            }

            if (TryApplyUnityUiExtensionsGradient(target, top, bottom))
            {
                target.color = Color.white;
                target.SetAllDirty();
                return;
            }

            target.sprite = GetOrCreateGradientSprite(top, bottom);
            target.type = Image.Type.Simple;
            target.color = Color.white;
            target.SetAllDirty();
        }

        public static void FitToSafeArea(RectTransform rect, float horizontalPadding, float topPadding, float bottomPadding)
        {
            if (!rect) return;

            float width = Screen.width;
            float height = Screen.height;

            if (width <= 0f || height <= 0f)
            {
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = new Vector2(horizontalPadding, bottomPadding);
                rect.offsetMax = new Vector2(-horizontalPadding, -topPadding);
                return;
            }

            Rect safeArea = Screen.safeArea;
            Vector2 anchorMin = safeArea.position;
            Vector2 anchorMax = safeArea.position + safeArea.size;
            anchorMin.x /= width;
            anchorMin.y /= height;
            anchorMax.x /= width;
            anchorMax.y /= height;

            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = new Vector2(horizontalPadding, bottomPadding);
            rect.offsetMax = new Vector2(-horizontalPadding, -topPadding);
        }

        public static void TryAttachSafeAreaComponent(GameObject go)
        {
            if (!go) return;

            string[] candidates =
            {
                "SafeAreaHelper.SafeArea",
                "SafeAreaHelper.SafeAreaPanel",
                "SafeAreaHelper.SafeAreaRect",
                "SafeArea.SafeArea",
                "SafeArea.Sizer"
            };

            foreach (var candidate in candidates)
            {
                if (TryAddOptionalComponent(go, candidate) != null)
                {
                    return;
                }
            }
        }

        public static void TryApplyRoundedCorners(GameObject go, float radius)
        {
            if (!go) return;

            var roundedMask = TryAddOptionalComponent(go, "UnityEngine.UI.Extensions.RoundedRectMask2D");
            if (roundedMask != null)
            {
                TrySetMember(roundedMask, new[] { "cornerRadius", "CornerRadius" }, radius);
                return;
            }

            var softMask = TryAddOptionalComponent(go, "Coffee.UIExtensions.UISoftMask");
            if (softMask != null)
            {
                TrySetMember(softMask, new[] { "softness", "Softness" }, 1f);
            }
        }

        private static Sprite GetOrCreateGradientSprite(Color top, Color bottom)
        {
            string key = GradientKey(top, bottom);
            if (GradientSpriteCache.TryGetValue(key, out var sprite))
                return sprite;

            const int height = 128;
            Texture2D tex = new Texture2D(1, height, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp
            };

            for (int i = 0; i < height; i++)
            {
                float t = i / (height - 1f);
                tex.SetPixel(0, i, Color.Lerp(bottom, top, t));
            }

            tex.Apply();
            GeneratedTextures.Add(tex);

            sprite = Sprite.Create(tex, new Rect(0f, 0f, 1f, height), new Vector2(0.5f, 0.5f));
            sprite.name = $"Gradient_{key}";
            GradientSpriteCache[key] = sprite;
            return sprite;
        }

        private static string GradientKey(Color top, Color bottom)
        {
            Color32 t = top;
            Color32 b = bottom;
            return $"{t.r}-{t.g}-{t.b}-{t.a}|{b.r}-{b.g}-{b.b}-{b.a}";
        }

        private static Component TryAddOptionalComponent(GameObject go, string typeName)
        {
            var type = FindType(typeName);
            if (type == null) return null;

            var existing = go.GetComponent(type);
            return existing ?? go.AddComponent(type);
        }

        private static Type FindType(string typeName)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    var type = assembly.GetType(typeName);
                    if (type != null)
                        return type;
                }
                catch
                {
                    // Optional assemblies may throw reflection errors – ignore them.
                }
            }

            return null;
        }

        private static bool TryApplyMobSakaiGradient(Image target, Color top, Color bottom)
        {
            var component = TryAddOptionalComponent(target.gameObject, "Coffee.UIExtensions.UIGradient");
            if (component == null)
                return false;

            try
            {
                var type = component.GetType();
                var gradientProperty = type.GetProperty("effectGradient", MemberFlags);
                if (gradientProperty != null && gradientProperty.CanWrite && gradientProperty.PropertyType == typeof(Gradient))
                {
                    gradientProperty.SetValue(component, BuildGradient(top, bottom));
                }
                else
                {
                    TrySetMember(component, new[] { "m_color1", "color1", "Color1" }, top);
                    TrySetMember(component, new[] { "m_color2", "color2", "Color2" }, bottom);
                }

                TrySetMember(component, new[] { "rotation", "Rotation" }, 90f);
                TrySetMember(component, new[] { "ignoreAspectRatio", "IgnoreAspectRatio" }, true);
                TrySetMember(component, new[] { "Direction", "direction" }, "Vertical");
            }
            catch
            {
                return false;
            }

            return true;
        }

        private static bool TryApplyUnityUiExtensionsGradient(Image target, Color top, Color bottom)
        {
            var component = TryAddOptionalComponent(target.gameObject, "UnityEngine.UI.Extensions.UIGradient");
            if (component == null)
                return false;

            try
            {
                TrySetMember(component, new[] { "Color1", "color1", "m_color1" }, top);
                TrySetMember(component, new[] { "Color2", "color2", "m_color2" }, bottom);
                if (!TrySetMember(component, new[] { "Direction", "direction", "gradientDirection" }, "Vertical"))
                {
                    TrySetMember(component, new[] { "Angle", "angle" }, 90f);
                }

                TrySetMember(component, new[] { "IgnoreAspectRatio", "ignoreAspectRatio" }, true);
            }
            catch
            {
                return false;
            }

            return true;
        }

        private static bool TrySetMember(object target, string[] names, object value)
        {
            if (target == null) return false;

            var type = target.GetType();
            foreach (var name in names)
            {
                var prop = type.GetProperty(name, MemberFlags);
                if (prop != null && prop.CanWrite)
                {
                    object converted = ConvertValue(value, prop.PropertyType);
                    if (converted != null)
                    {
                        prop.SetValue(target, converted);
                        return true;
                    }
                }

                var field = type.GetField(name, MemberFlags);
                if (field != null)
                {
                    object converted = ConvertValue(value, field.FieldType);
                    if (converted != null)
                    {
                        field.SetValue(target, converted);
                        return true;
                    }
                }
            }

            return false;
        }

        private static object ConvertValue(object value, Type targetType)
        {
            if (value == null || targetType == null)
                return null;

            if (targetType.IsInstanceOfType(value))
                return value;

            if (targetType == typeof(Color))
            {
                if (value is Color c) return c;
                if (value is Color32 c32) return (Color)c32;
            }

            if (targetType == typeof(Color32))
            {
                if (value is Color c) return (Color32)c;
                if (value is Color32 c32) return c32;
            }

            if (targetType == typeof(float))
            {
                if (value is float f) return f;
                if (value is double d) return (float)d;
                if (value is int i) return i;
            }

            if (targetType == typeof(int))
            {
                if (value is float f) return Mathf.RoundToInt(f);
                if (value is double d) return (int)d;
                if (value is int i) return i;
            }

            if (targetType == typeof(bool))
            {
                if (value is bool b) return b;
                if (value is int i) return i != 0;
            }

            if (targetType == typeof(Vector4))
            {
                if (value is float f) return new Vector4(f, f, f, f);
                if (value is Vector4 v4) return v4;
            }

            if (targetType.IsEnum)
            {
                if (value is string s)
                {
                    foreach (var name in Enum.GetNames(targetType))
                    {
                        if (string.Equals(name, s, StringComparison.OrdinalIgnoreCase))
                            return Enum.Parse(targetType, name);
                    }
                }

                if (value is int i)
                {
                    return Enum.ToObject(targetType, i);
                }
            }

            return null;
        }

        private static Gradient BuildGradient(Color top, Color bottom)
        {
            var gradient = new Gradient
            {
                colorKeys = new[]
                {
                    new GradientColorKey(top, 0f),
                    new GradientColorKey(bottom, 1f)
                },
                alphaKeys = new[]
                {
                    new GradientAlphaKey(top.a, 0f),
                    new GradientAlphaKey(bottom.a, 1f)
                }
            };

            return gradient;
        }
    }
}
