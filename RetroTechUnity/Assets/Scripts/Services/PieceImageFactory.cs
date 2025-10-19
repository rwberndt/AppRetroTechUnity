using System;
using System.Collections.Generic;
using UnityEngine;
using RetroTech;

namespace RetroTech.Services
{
    /// <summary>
    /// Provides cached conversion from <see cref="ComputerPiece"/> binary image data to
    /// <see cref="Sprite"/> instances that can be consumed by Unity UI components.
    /// </summary>
    public static class PieceImageFactory
    {
        private static readonly Dictionary<long, Sprite> SpriteCache = new();

        /// <summary>
        /// Creates (and caches) a sprite using the image data contained in the provided piece.
        /// </summary>
        /// <param name="piece">The domain entity containing the raw image data.</param>
        /// <returns>A sprite when image data is available; otherwise <c>null</c>.</returns>
        public static Sprite GetSprite(ComputerPiece piece)
        {
            if (piece == null || !piece.HasImageData)
            {
                return null;
            }

            if (SpriteCache.TryGetValue(piece.Id, out Sprite cached) && cached != null)
            {
                return cached;
            }

            try
            {
                var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!texture.LoadImage(piece.ImageData))
                {
                    UnityEngine.Object.Destroy(texture);
                    return null;
                }

                texture.wrapMode = TextureWrapMode.Clamp;
                texture.filterMode = FilterMode.Bilinear;

                var sprite = Sprite.Create(
                    texture,
                    new Rect(0, 0, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f));

                SpriteCache[piece.Id] = sprite;
                return sprite;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
