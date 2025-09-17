using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace RetroTech.Services
{
    /// <summary>
    /// Defines a minimal HTTP client abstraction used to communicate with the RetroTech API.
    /// </summary>
    public interface IApiClient
    {
        /// <summary>
        /// Executes a GET request against the specified endpoint and deserializes the
        /// response into the requested type.
        /// </summary>
        Task<T> GetAsync<T>(string endpoint, CancellationToken cancellationToken = default);

        /// <summary>
        /// Executes a GET request and returns a collection of strongly typed items.
        /// </summary>
        Task<IReadOnlyList<T>> GetCollectionAsync<T>(string endpoint, CancellationToken cancellationToken = default);
    }
}
