using System.Collections.Generic;

namespace RetroTech.Services
{
    /// <summary>
    /// Small abstraction over JSON deserialization to make the HTTP client easier to test and
    /// to allow us to swap out the implementation if required.
    /// </summary>
    public interface IJsonSerializer
    {
        T Deserialize<T>(string json);

        IReadOnlyList<T> DeserializeCollection<T>(string json);
    }
}
