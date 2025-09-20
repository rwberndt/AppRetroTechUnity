using System;
using System.Collections.Generic;
using UnityEngine;

namespace RetroTech.Services
{
    /// <summary>
    /// JSON serializer that relies on Unity's <see cref="JsonUtility"/> so we avoid bringing
    /// additional dependencies into the project while still being able to parse arrays.
    /// </summary>
    public class UnityJsonSerializer : IJsonSerializer
    {
        public T Deserialize<T>(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return default;
            }

            string trimmed = json.Trim();

            try
            {
                return JsonUtility.FromJson<T>(trimmed);
            }
            catch (ArgumentException)
            {
                return DeserializeFromWrapper<T>(trimmed);
            }
        }

        public IReadOnlyList<T> DeserializeCollection<T>(string json)
        {
            var result = new List<T>();
            if (string.IsNullOrWhiteSpace(json))
            {
                return result;
            }

            string trimmed = json.Trim();

            if (trimmed.StartsWith("["))
            {
                result.AddRange(JsonArrayHelper.FromJson<T>(trimmed));
                return result;
            }

            try
            {
                var wrapper = JsonUtility.FromJson<CollectionWrapper<T>>(trimmed);
                if (wrapper != null)
                {
                    if (wrapper.data != null) result.AddRange(wrapper.data);
                    if (wrapper.items != null) result.AddRange(wrapper.items);
                    if (wrapper.results != null) result.AddRange(wrapper.results);
                }
            }
            catch (ArgumentException)
            {
                // Ignore and try extracting manually below.
            }

            if (result.Count > 0)
            {
                return result;
            }

            string arraySegment = JsonArrayHelper.TryExtractArraySegment(trimmed);
            if (!string.IsNullOrEmpty(arraySegment))
            {
                result.AddRange(JsonArrayHelper.FromJson<T>(arraySegment));
                return result;
            }

            // Fallback: treat response as a single object and wrap it into a list.
            T single = Deserialize<T>(trimmed);
            if (!EqualityComparer<T>.Default.Equals(single, default))
            {
                result.Add(single);
            }

            return result;
        }

        private static T DeserializeFromWrapper<T>(string json)
        {
            try
            {
                var wrapper = JsonUtility.FromJson<ObjectWrapper<T>>(json);
                if (wrapper != null)
                {
                    if (!EqualityComparer<T>.Default.Equals(wrapper.data, default))
                        return wrapper.data;
                    if (!EqualityComparer<T>.Default.Equals(wrapper.item, default))
                        return wrapper.item;
                    if (!EqualityComparer<T>.Default.Equals(wrapper.result, default))
                        return wrapper.result;
                }
            }
            catch (ArgumentException)
            {
                // ignored
            }

            return default;
        }

        [Serializable]
        private class CollectionWrapper<T>
        {
            public T[] data;
            public T[] items;
            public T[] results;
        }

        [Serializable]
        private class ObjectWrapper<T>
        {
            public T data;
            public T item;
            public T result;
        }
    }
}
