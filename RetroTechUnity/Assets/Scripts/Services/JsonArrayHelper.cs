using System;
using System.Collections.Generic;
using UnityEngine;

namespace RetroTech.Services
{
    /// <summary>
    /// Helper utilities to bridge Unity's <see cref="JsonUtility"/> limitations when parsing
    /// JSON arrays directly.
    /// </summary>
    public static class JsonArrayHelper
    {
        public static List<T> FromJson<T>(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return new List<T>();
            }

            string wrapped = $"{{\"items\":{json}}}";
            var wrapper = JsonUtility.FromJson<Wrapper<T>>(wrapped);
            if (wrapper?.items == null)
            {
                return new List<T>();
            }

            return new List<T>(wrapper.items);
        }

        public static string TryExtractArraySegment(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            int startIndex = json.IndexOf('[');
            int endIndex = json.LastIndexOf(']');
            if (startIndex < 0 || endIndex <= startIndex)
            {
                return null;
            }

            return json.Substring(startIndex, endIndex - startIndex + 1);
        }

        [Serializable]
        private class Wrapper<T>
        {
            public T[] items;
        }
    }
}
