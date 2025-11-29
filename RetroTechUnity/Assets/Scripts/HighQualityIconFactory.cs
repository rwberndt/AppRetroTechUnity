using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Factory for generating high-quality, crisp vector-style navigation icons
/// Provides anti-aliased, scalable icons that look sharp at any size
/// </summary>
public static class HighQualityIconFactory
{
    private const int BaseResolution = 256; // High resolution for crisp rendering
    private const float StrokeWidth = 16f;
    private const float AntiAliasWidth = 2f;

    /// <summary>
    /// Creates a high-quality home icon with smooth lines
    /// </summary>
    public static Sprite CreateHomeIcon()
    {
        Texture2D tex = new Texture2D(BaseResolution, BaseResolution, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        Color[] pixels = new Color[BaseResolution * BaseResolution];

        // Clear background
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = Color.clear;

        // Draw house shape with anti-aliasing
        Vector2 center = new Vector2(BaseResolution / 2f, BaseResolution / 2f);
        float houseWidth = BaseResolution * 0.65f;
        float houseHeight = BaseResolution * 0.55f;
        float roofHeight = BaseResolution * 0.35f;

        // Roof triangle (top part)
        Vector2 roofTop = new Vector2(center.x, center.y + houseHeight / 2f + roofHeight);
        Vector2 roofLeft = new Vector2(center.x - houseWidth / 2f, center.y + houseHeight / 2f);
        Vector2 roofRight = new Vector2(center.x + houseWidth / 2f, center.y + houseHeight / 2f);

        // House body rectangle
        Vector2 houseTopLeft = new Vector2(center.x - houseWidth / 2.5f, center.y + houseHeight / 2f);
        Vector2 houseBottomLeft = new Vector2(center.x - houseWidth / 2.5f, center.y - houseHeight / 2f);
        Vector2 houseTopRight = new Vector2(center.x + houseWidth / 2.5f, center.y + houseHeight / 2f);
        Vector2 houseBottomRight = new Vector2(center.x + houseWidth / 2.5f, center.y - houseHeight / 2f);

        // Door
        float doorWidth = houseWidth / 4.5f;
        float doorHeight = houseHeight / 2.2f;
        Vector2 doorTopLeft = new Vector2(center.x - doorWidth / 2f, center.y);
        Vector2 doorBottomRight = new Vector2(center.x + doorWidth / 2f, center.y - houseHeight / 2f);

        // Draw all shapes with anti-aliasing
        DrawThickLine(pixels, BaseResolution, roofLeft, roofTop, StrokeWidth, Color.white);
        DrawThickLine(pixels, BaseResolution, roofTop, roofRight, StrokeWidth, Color.white);
        DrawThickLine(pixels, BaseResolution, houseTopLeft, houseBottomLeft, StrokeWidth, Color.white);
        DrawThickLine(pixels, BaseResolution, houseTopRight, houseBottomRight, StrokeWidth, Color.white);
        DrawThickLine(pixels, BaseResolution, houseBottomLeft, houseBottomRight, StrokeWidth, Color.white);

        // Door outline
        DrawThickLine(pixels, BaseResolution, doorTopLeft, new Vector2(doorTopLeft.x, doorBottomRight.y), StrokeWidth * 0.7f, Color.white);
        DrawThickLine(pixels, BaseResolution, new Vector2(doorBottomRight.x, doorTopLeft.y), doorBottomRight, StrokeWidth * 0.7f, Color.white);
        DrawThickLine(pixels, BaseResolution, doorTopLeft, new Vector2(doorBottomRight.x, doorTopLeft.y), StrokeWidth * 0.7f, Color.white);

        tex.SetPixels(pixels);
        tex.Apply();

        return Sprite.Create(tex, new Rect(0, 0, BaseResolution, BaseResolution), new Vector2(0.5f, 0.5f), 100);
    }

    /// <summary>
    /// Creates a high-quality grid icon for categories
    /// </summary>
    public static Sprite CreateCategoriesIcon()
    {
        Texture2D tex = new Texture2D(BaseResolution, BaseResolution, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        Color[] pixels = new Color[BaseResolution * BaseResolution];

        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = Color.clear;

        // Draw a 3x3 grid of rounded squares
        float gridSize = BaseResolution * 0.7f;
        float cellSize = gridSize / 3.5f;
        float gap = gridSize * 0.08f;
        float startX = (BaseResolution - gridSize) / 2f + cellSize / 2f;
        float startY = (BaseResolution - gridSize) / 2f + cellSize / 2f;

        for (int row = 0; row < 3; row++)
        {
            for (int col = 0; col < 3; col++)
            {
                float x = startX + col * (cellSize + gap);
                float y = startY + row * (cellSize + gap);
                DrawRoundedRect(pixels, BaseResolution, new Vector2(x, y), cellSize * 0.9f, cellSize * 0.9f, cellSize * 0.2f, Color.white);
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();

        return Sprite.Create(tex, new Rect(0, 0, BaseResolution, BaseResolution), new Vector2(0.5f, 0.5f), 100);
    }

    /// <summary>
    /// Creates a high-quality timeline icon with smooth curves
    /// </summary>
    public static Sprite CreateTimelineIcon()
    {
        Texture2D tex = new Texture2D(BaseResolution, BaseResolution, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        Color[] pixels = new Color[BaseResolution * BaseResolution];

        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = Color.clear;

        Vector2 center = new Vector2(BaseResolution / 2f, BaseResolution / 2f);
        float lineLength = BaseResolution * 0.7f;

        // Vertical timeline line
        Vector2 lineStart = new Vector2(center.x - lineLength * 0.3f, center.y - lineLength / 2f);
        Vector2 lineEnd = new Vector2(center.x - lineLength * 0.3f, center.y + lineLength / 2f);
        DrawThickLine(pixels, BaseResolution, lineStart, lineEnd, StrokeWidth * 0.8f, Color.white);

        // Timeline nodes
        float[] nodePositions = { -0.35f, -0.05f, 0.25f };
        float[] nodeSizes = { 20f, 16f, 20f };

        for (int i = 0; i < nodePositions.Length; i++)
        {
            Vector2 nodePos = new Vector2(center.x - lineLength * 0.3f, center.y + lineLength * nodePositions[i]);
            DrawCircle(pixels, BaseResolution, nodePos, nodeSizes[i], Color.white);

            // Connection lines to the right
            Vector2 lineConnectionEnd = new Vector2(center.x + lineLength * 0.25f, nodePos.y);
            DrawThickLine(pixels, BaseResolution, nodePos, lineConnectionEnd, StrokeWidth * 0.6f, Color.white);
        }

        tex.SetPixels(pixels);
        tex.Apply();

        return Sprite.Create(tex, new Rect(0, 0, BaseResolution, BaseResolution), new Vector2(0.5f, 0.5f), 100);
    }

    /// <summary>
    /// Creates a high-quality QR code scanner icon
    /// </summary>
    public static Sprite CreateScannerIcon()
    {
        Texture2D tex = new Texture2D(BaseResolution, BaseResolution, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        Color[] pixels = new Color[BaseResolution * BaseResolution];

        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = Color.clear;

        Vector2 center = new Vector2(BaseResolution / 2f, BaseResolution / 2f);
        float frameSize = BaseResolution * 0.65f;
        float cornerLength = frameSize * 0.25f;
        float cornerThickness = StrokeWidth;

        // Draw four corners of QR frame
        // Top-left corner
        DrawThickLine(pixels, BaseResolution,
            new Vector2(center.x - frameSize / 2f, center.y + frameSize / 2f),
            new Vector2(center.x - frameSize / 2f + cornerLength, center.y + frameSize / 2f),
            cornerThickness, Color.white);
        DrawThickLine(pixels, BaseResolution,
            new Vector2(center.x - frameSize / 2f, center.y + frameSize / 2f),
            new Vector2(center.x - frameSize / 2f, center.y + frameSize / 2f - cornerLength),
            cornerThickness, Color.white);

        // Top-right corner
        DrawThickLine(pixels, BaseResolution,
            new Vector2(center.x + frameSize / 2f, center.y + frameSize / 2f),
            new Vector2(center.x + frameSize / 2f - cornerLength, center.y + frameSize / 2f),
            cornerThickness, Color.white);
        DrawThickLine(pixels, BaseResolution,
            new Vector2(center.x + frameSize / 2f, center.y + frameSize / 2f),
            new Vector2(center.x + frameSize / 2f, center.y + frameSize / 2f - cornerLength),
            cornerThickness, Color.white);

        // Bottom-left corner
        DrawThickLine(pixels, BaseResolution,
            new Vector2(center.x - frameSize / 2f, center.y - frameSize / 2f),
            new Vector2(center.x - frameSize / 2f + cornerLength, center.y - frameSize / 2f),
            cornerThickness, Color.white);
        DrawThickLine(pixels, BaseResolution,
            new Vector2(center.x - frameSize / 2f, center.y - frameSize / 2f),
            new Vector2(center.x - frameSize / 2f, center.y - frameSize / 2f + cornerLength),
            cornerThickness, Color.white);

        // Bottom-right corner
        DrawThickLine(pixels, BaseResolution,
            new Vector2(center.x + frameSize / 2f, center.y - frameSize / 2f),
            new Vector2(center.x + frameSize / 2f - cornerLength, center.y - frameSize / 2f),
            cornerThickness, Color.white);
        DrawThickLine(pixels, BaseResolution,
            new Vector2(center.x + frameSize / 2f, center.y - frameSize / 2f),
            new Vector2(center.x + frameSize / 2f, center.y - frameSize / 2f + cornerLength),
            cornerThickness, Color.white);

        // Scanning line
        DrawThickLine(pixels, BaseResolution,
            new Vector2(center.x - frameSize / 2.5f, center.y),
            new Vector2(center.x + frameSize / 2.5f, center.y),
            StrokeWidth * 0.5f, Color.white);

        tex.SetPixels(pixels);
        tex.Apply();

        return Sprite.Create(tex, new Rect(0, 0, BaseResolution, BaseResolution), new Vector2(0.5f, 0.5f), 100);
    }

    /// <summary>
    /// Creates a high-quality quiz icon (question mark in circle)
    /// </summary>
    public static Sprite CreateQuizIcon()
    {
        Texture2D tex = new Texture2D(BaseResolution, BaseResolution, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        Color[] pixels = new Color[BaseResolution * BaseResolution];

        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = Color.clear;

        Vector2 center = new Vector2(BaseResolution / 2f, BaseResolution / 2f);
        float circleRadius = BaseResolution * 0.35f;

        // Draw circle outline
        DrawCircleOutline(pixels, BaseResolution, center, circleRadius, StrokeWidth, Color.white);

        // Draw question mark
        float qHeight = BaseResolution * 0.4f;
        float qWidth = BaseResolution * 0.2f;

        // Top curve of question mark
        Vector2 qTop = new Vector2(center.x, center.y + qHeight / 3f);
        Vector2 qRight = new Vector2(center.x + qWidth / 2f, center.y + qHeight / 6f);
        Vector2 qMid = new Vector2(center.x, center.y);

        DrawBezierCurve(pixels, BaseResolution,
            new Vector2(center.x - qWidth / 2f, qTop.y),
            new Vector2(center.x + qWidth / 1.5f, qTop.y),
            qRight,
            StrokeWidth * 0.9f, Color.white);

        DrawBezierCurve(pixels, BaseResolution,
            qRight,
            new Vector2(center.x + qWidth / 2f, center.y),
            qMid,
            StrokeWidth * 0.9f, Color.white);

        // Bottom dot
        DrawCircle(pixels, BaseResolution, new Vector2(center.x, center.y - qHeight / 3.5f), StrokeWidth * 0.7f, Color.white);

        tex.SetPixels(pixels);
        tex.Apply();

        return Sprite.Create(tex, new Rect(0, 0, BaseResolution, BaseResolution), new Vector2(0.5f, 0.5f), 100);
    }

    // === Drawing Helper Methods ===

    private static void DrawThickLine(Color[] pixels, int width, Vector2 start, Vector2 end, float thickness, Color color)
    {
        float length = Vector2.Distance(start, end);
        Vector2 direction = (end - start).normalized;
        Vector2 perpendicular = new Vector2(-direction.y, direction.x);

        for (float t = 0; t <= length; t += 0.5f)
        {
            Vector2 point = start + direction * t;

            for (float w = -thickness / 2f; w <= thickness / 2f; w += 0.5f)
            {
                Vector2 pixelPos = point + perpendicular * w;
                int x = Mathf.RoundToInt(pixelPos.x);
                int y = Mathf.RoundToInt(pixelPos.y);

                if (x >= 0 && x < width && y >= 0 && y < width)
                {
                    int index = y * width + x;
                    if (index >= 0 && index < pixels.Length)
                    {
                        // Anti-aliasing
                        float dist = Mathf.Abs(w);
                        float alpha = 1f - Mathf.Clamp01((dist - thickness / 2f + AntiAliasWidth) / AntiAliasWidth);
                        Color currentColor = pixels[index];
                        pixels[index] = Color.Lerp(currentColor, color, alpha * color.a);
                    }
                }
            }
        }
    }

    private static void DrawCircle(Color[] pixels, int width, Vector2 center, float radius, Color color)
    {
        int minX = Mathf.Max(0, Mathf.FloorToInt(center.x - radius - AntiAliasWidth));
        int maxX = Mathf.Min(width - 1, Mathf.CeilToInt(center.x + radius + AntiAliasWidth));
        int minY = Mathf.Max(0, Mathf.FloorToInt(center.y - radius - AntiAliasWidth));
        int maxY = Mathf.Min(width - 1, Mathf.CeilToInt(center.y + radius + AntiAliasWidth));

        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);

                if (dist <= radius + AntiAliasWidth)
                {
                    float alpha = 1f;
                    if (dist > radius - AntiAliasWidth)
                    {
                        alpha = 1f - Mathf.Clamp01((dist - radius + AntiAliasWidth) / (2f * AntiAliasWidth));
                    }

                    int index = y * width + x;
                    if (index >= 0 && index < pixels.Length)
                    {
                        Color currentColor = pixels[index];
                        pixels[index] = Color.Lerp(currentColor, color, alpha * color.a);
                    }
                }
            }
        }
    }

    private static void DrawCircleOutline(Color[] pixels, int width, Vector2 center, float radius, float thickness, Color color)
    {
        int minX = Mathf.Max(0, Mathf.FloorToInt(center.x - radius - thickness - AntiAliasWidth));
        int maxX = Mathf.Min(width - 1, Mathf.CeilToInt(center.x + radius + thickness + AntiAliasWidth));
        int minY = Mathf.Max(0, Mathf.FloorToInt(center.y - radius - thickness - AntiAliasWidth));
        int maxY = Mathf.Min(width - 1, Mathf.CeilToInt(center.y + radius + thickness + AntiAliasWidth));

        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                float distFromEdge = Mathf.Abs(dist - radius);

                if (distFromEdge <= thickness / 2f + AntiAliasWidth)
                {
                    float alpha = 1f - Mathf.Clamp01((distFromEdge - thickness / 2f) / AntiAliasWidth);

                    int index = y * width + x;
                    if (index >= 0 && index < pixels.Length)
                    {
                        Color currentColor = pixels[index];
                        pixels[index] = Color.Lerp(currentColor, color, alpha * color.a);
                    }
                }
            }
        }
    }

    private static void DrawRoundedRect(Color[] pixels, int width, Vector2 center, float rectWidth, float rectHeight, float cornerRadius, Color color)
    {
        int minX = Mathf.Max(0, Mathf.FloorToInt(center.x - rectWidth / 2f - AntiAliasWidth));
        int maxX = Mathf.Min(width - 1, Mathf.CeilToInt(center.x + rectWidth / 2f + AntiAliasWidth));
        int minY = Mathf.Max(0, Mathf.FloorToInt(center.y - rectHeight / 2f - AntiAliasWidth));
        int maxY = Mathf.Min(width - 1, Mathf.CeilToInt(center.y + rectHeight / 2f + AntiAliasWidth));

        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                Vector2 point = new Vector2(x, y);
                Vector2 localPoint = point - center;

                float dx = Mathf.Max(0, Mathf.Abs(localPoint.x) - (rectWidth / 2f - cornerRadius));
                float dy = Mathf.Max(0, Mathf.Abs(localPoint.y) - (rectHeight / 2f - cornerRadius));
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                bool inside = false;
                float edgeDist = 0;

                if (dx == 0 && dy == 0)
                {
                    inside = true;
                }
                else if (dx > 0 && dy > 0)
                {
                    edgeDist = dist - cornerRadius;
                    inside = dist <= cornerRadius;
                }
                else
                {
                    edgeDist = Mathf.Max(dx, dy);
                    inside = edgeDist <= 0;
                }

                if (inside || edgeDist <= AntiAliasWidth)
                {
                    float alpha = inside ? 1f : 1f - Mathf.Clamp01(edgeDist / AntiAliasWidth);

                    int index = y * width + x;
                    if (index >= 0 && index < pixels.Length)
                    {
                        Color currentColor = pixels[index];
                        pixels[index] = Color.Lerp(currentColor, color, alpha * color.a);
                    }
                }
            }
        }
    }

    private static void DrawBezierCurve(Color[] pixels, int width, Vector2 p0, Vector2 p1, Vector2 p2, float thickness, Color color)
    {
        int steps = 50;
        Vector2 prevPoint = p0;

        for (int i = 1; i <= steps; i++)
        {
            float t = i / (float)steps;
            Vector2 point = CalculateBezierPoint(t, p0, p1, p2);
            DrawThickLine(pixels, width, prevPoint, point, thickness, color);
            prevPoint = point;
        }
    }

    private static Vector2 CalculateBezierPoint(float t, Vector2 p0, Vector2 p1, Vector2 p2)
    {
        float u = 1 - t;
        float tt = t * t;
        float uu = u * u;

        Vector2 point = uu * p0;
        point += 2 * u * t * p1;
        point += tt * p2;

        return point;
    }
}
