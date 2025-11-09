using System;
using UnityEngine;

namespace RetroTech
{
    /// <summary>
    /// Provides access to common UI icons while gracefully falling back to
    /// procedurally generated sprites when the project doesn't include
    /// specific assets in the Resources/Icons directory.
    /// </summary>
    public static class IconFactory
    {
        private static Sprite _backIcon;
        private static Sprite _closeIcon;

        /// <summary>
        /// Gets an icon representing a back arrow. Attempts to load
        /// a sprite named "icon_back" from Resources/Icons before
        /// generating a fallback texture.
        /// </summary>
        public static Sprite GetBackIcon()
        {
            return _backIcon ??= LoadOrCreateIcon("icon_back", CreateBackIcon);
        }

        /// <summary>
        /// Gets an icon representing a close action. Attempts to load
        /// a sprite named "icon_close" from Resources/Icons before
        /// generating a fallback texture.
        /// </summary>
        public static Sprite GetCloseIcon()
        {
            return _closeIcon ??= LoadOrCreateIcon("icon_close", CreateCloseIcon);
        }

        private static Sprite LoadOrCreateIcon(string resourceName, Func<Sprite> fallbackFactory)
        {
            var sprite = Resources.Load<Sprite>($"Icons/{resourceName}");
            if (sprite != null)
            {
                return sprite;
            }

            sprite = fallbackFactory?.Invoke();
            if (sprite != null)
            {
                sprite.name = $"generated_{resourceName}";
            }
            return sprite;
        }

        private static Sprite CreateBackIcon()
        {
            const int size = 64;
            var texture = CreateClearTexture(size, size);

            DrawBackArrow(texture, Color.white);

            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        private static Sprite CreateCloseIcon()
        {
            const int size = 64;
            var texture = CreateClearTexture(size, size);

            DrawCloseMark(texture, Color.white);

            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        private static Texture2D CreateClearTexture(int width, int height)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            var pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.clear;
            }

            texture.SetPixels(pixels);
            return texture;
        }

        private static void DrawBackArrow(Texture2D texture, Color color)
        {
            int width = texture.width;
            int height = texture.height;
            int centerY = height / 2;

            int shaftStartX = width / 2;
            int shaftEndX = width - width / 6;
            int shaftTopY = height / 4;
            int shaftBottomY = height - height / 4;

            for (int y = shaftTopY; y < shaftBottomY; y++)
            {
                for (int x = shaftStartX; x < shaftEndX; x++)
                {
                    texture.SetPixel(x, y, color);
                }
            }

            int headMaxWidth = width / 3;
            int headStartX = width / 6;
            for (int y = height / 6; y < height - height / 6; y++)
            {
                int distance = Mathf.Abs(y - centerY);
                int headWidth = Mathf.Clamp(headMaxWidth - distance, 0, headMaxWidth);
                int headEndX = shaftStartX;
                int headCurrentStartX = Mathf.Max(headStartX, headEndX - headWidth);

                for (int x = headCurrentStartX; x < headEndX; x++)
                {
                    texture.SetPixel(x, y, color);
                }
            }
        }

        private static void DrawCloseMark(Texture2D texture, Color color)
        {
            int width = texture.width;
            int height = texture.height;
            int thickness = Mathf.Max(2, width / 16);
            int margin = width / 6;

            for (int i = margin; i < width - margin; i++)
            {
                DrawThickPixel(texture, i, i, thickness, color);
                DrawThickPixel(texture, i, height - 1 - i, thickness, color);
            }
        }

        private static void DrawThickPixel(Texture2D texture, int x, int y, int thickness, Color color)
        {
            int half = thickness / 2;
            for (int offsetX = -half; offsetX <= half; offsetX++)
            {
                for (int offsetY = -half; offsetY <= half; offsetY++)
                {
                    int px = x + offsetX;
                    int py = y + offsetY;
                    if (px >= 0 && px < texture.width && py >= 0 && py < texture.height)
                    {
                        texture.SetPixel(px, py, color);
                    }
                }
            }
        }
    }
}
