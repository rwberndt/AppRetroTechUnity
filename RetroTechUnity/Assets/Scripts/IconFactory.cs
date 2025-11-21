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
        private static Sprite _homeIcon;
        private static Sprite _categoriesIcon;
        private static Sprite _timelineIcon;
        private static Sprite _scannerIcon;
        private static Sprite _quizIcon;

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

        /// <summary>
        /// Gets a navigation icon representing the Home tab.
        /// </summary>
        public static Sprite GetHomeIcon()
        {
            return _homeIcon ??= LoadOrCreateIcon("icon_home", CreateHomeIcon);
        }

        /// <summary>
        /// Gets a navigation icon representing the Categories tab.
        /// </summary>
        public static Sprite GetCategoriesIcon()
        {
            return _categoriesIcon ??= LoadOrCreateIcon("icon_categories", CreateCategoriesIcon);
        }

        /// <summary>
        /// Gets a navigation icon representing the Timeline tab.
        /// </summary>
        public static Sprite GetTimelineIcon()
        {
            return _timelineIcon ??= LoadOrCreateIcon("icon_timeline", CreateTimelineIcon);
        }

        /// <summary>
        /// Gets a navigation icon representing the QR scanner tab.
        /// </summary>
        public static Sprite GetScannerIcon()
        {
            return _scannerIcon ??= LoadOrCreateIcon("icon_scanner", CreateScannerIcon);
        }

        /// <summary>
        /// Gets a navigation icon representing the Quiz tab.
        /// </summary>
        public static Sprite GetQuizIcon()
        {
            return _quizIcon ??= LoadOrCreateIcon("icon_quiz", CreateQuizIcon);
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

        private static Sprite CreateHomeIcon()
        {
            const int size = 96;
            var tex = CreateClearTexture(size, size);

            // Draw roof
            for (int y = size / 3; y < size / 2 + size / 6; y++)
            {
                int span = (y - size / 3) * 2 + size / 6;
                int startX = size / 2 - span / 2;
                int endX = size / 2 + span / 2;
                for (int x = startX; x <= endX; x++)
                {
                    tex.SetPixel(x, y, Color.white);
                }
            }

            // Draw house body
            DrawRect(tex, size / 3, size / 2, size - size / 3, size - size / 6, Color.white);

            // Door
            DrawRect(tex, size / 2 - size / 10, size / 2, size / 2 + size / 10, size - size / 6, new Color(0.8f, 0.78f, 1f));

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        private static Sprite CreateCategoriesIcon()
        {
            const int size = 96;
            var tex = CreateClearTexture(size, size);
            int padding = size / 10;
            int cell = (size - padding * 3) / 2;

            DrawRoundedRect(tex, padding, padding, padding + cell, padding + cell, Color.white, 4);
            DrawRoundedRect(tex, padding * 2 + cell, padding, size - padding, padding + cell, Color.white, 4);
            DrawRoundedRect(tex, padding, padding * 2 + cell, padding + cell, size - padding, Color.white, 4);
            DrawRoundedRect(tex, padding * 2 + cell, padding * 2 + cell, size - padding, size - padding, Color.white, 4);

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        private static Sprite CreateTimelineIcon()
        {
            const int size = 96;
            var tex = CreateClearTexture(size, size);

            int centerY = size / 2;
            DrawRect(tex, size / 8, centerY - 3, size - size / 8, centerY + 3, new Color(0.85f, 0.85f, 1f));

            int[] stops = { size / 6, size / 2, size - size / 6 };
            foreach (int x in stops)
            {
                DrawCircle(tex, x, centerY, size / 10, Color.white);
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        private static Sprite CreateScannerIcon()
        {
            const int size = 96;
            var tex = CreateClearTexture(size, size);
            int margin = size / 8;
            int thickness = size / 16;

            // Corner brackets
            DrawRect(tex, margin, margin, margin + thickness, size / 2 - thickness, Color.white);
            DrawRect(tex, margin, margin, size / 2 - thickness, margin + thickness, Color.white);

            DrawRect(tex, size - margin - thickness, margin, size - margin, size / 2 - thickness, Color.white);
            DrawRect(tex, size / 2 + thickness, margin, size - margin, margin + thickness, Color.white);

            DrawRect(tex, margin, size / 2 + thickness, margin + thickness, size - margin, Color.white);
            DrawRect(tex, margin, size - margin - thickness, size / 2 - thickness, size - margin, Color.white);

            DrawRect(tex, size - margin - thickness, size / 2 + thickness, size - margin, size - margin, Color.white);
            DrawRect(tex, size / 2 + thickness, size - margin - thickness, size - margin, size - margin, Color.white);

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        private static Sprite CreateQuizIcon()
        {
            const int size = 96;
            var tex = CreateClearTexture(size, size);

            // Question mark stem
            DrawRect(tex, size / 2 - size / 16, size / 3, size / 2 + size / 16, size / 2 + size / 10, Color.white);
            DrawRect(tex, size / 2 - size / 8, size / 2 + size / 10, size / 2 + size / 8, size / 2 + size / 4, Color.white);

            // Hook
            DrawRoundedRect(tex, size / 2 - size / 4, size / 6, size / 2 + size / 4, size / 3 + size / 8, Color.white, 6);

            // Dot
            DrawCircle(tex, size / 2, size - size / 4, size / 12, Color.white);

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        private static void DrawRect(Texture2D tex, int xMin, int yMin, int xMax, int yMax, Color color)
        {
            for (int y = yMin; y <= yMax; y++)
            {
                for (int x = xMin; x <= xMax; x++)
                {
                    SetPixelSafe(tex, x, y, color);
                }
            }
        }

        private static void DrawRoundedRect(Texture2D tex, int xMin, int yMin, int xMax, int yMax, Color color, int radius)
        {
            for (int y = yMin; y <= yMax; y++)
            {
                for (int x = xMin; x <= xMax; x++)
                {
                    bool insideCorner =
                        (x < xMin + radius && y < yMin + radius && (x - (xMin + radius)) * (x - (xMin + radius)) + (y - (yMin + radius)) * (y - (yMin + radius)) > radius * radius) ||
                        (x > xMax - radius && y < yMin + radius && (x - (xMax - radius)) * (x - (xMax - radius)) + (y - (yMin + radius)) * (y - (yMin + radius)) > radius * radius) ||
                        (x < xMin + radius && y > yMax - radius && (x - (xMin + radius)) * (x - (xMin + radius)) + (y - (yMax - radius)) * (y - (yMax - radius)) > radius * radius) ||
                        (x > xMax - radius && y > yMax - radius && (x - (xMax - radius)) * (x - (xMax - radius)) + (y - (yMax - radius)) * (y - (yMax - radius)) > radius * radius);

                    if (!insideCorner)
                    {
                        SetPixelSafe(tex, x, y, color);
                    }
                }
            }
        }

        private static void DrawCircle(Texture2D tex, int centerX, int centerY, int radius, Color color)
        {
            int rSquared = radius * radius;
            for (int y = -radius; y <= radius; y++)
            {
                for (int x = -radius; x <= radius; x++)
                {
                    if (x * x + y * y <= rSquared)
                    {
                        SetPixelSafe(tex, centerX + x, centerY + y, color);
                    }
                }
            }
        }

        private static void SetPixelSafe(Texture2D tex, int x, int y, Color color)
        {
            if (x < 0 || y < 0 || x >= tex.width || y >= tex.height) return;
            tex.SetPixel(x, y, color);
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
