using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace RetroTech.Services
{
    /// <summary>
    /// Thin wrapper around <see cref="HttpClient"/> that centralises the base configuration and
    /// JSON parsing for the RetroTech application.
    /// </summary>
    public class ApiClient : IApiClient, IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly IJsonSerializer _serializer;
        private bool _disposed;

        public ApiClient(ApiConfiguration configuration, IJsonSerializer serializer = null)
        {
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));

            _serializer = serializer ?? new UnityJsonSerializer();
            _httpClient = CreateHttpClient(configuration);
        }

        public async Task<T> GetAsync<T>(string endpoint, CancellationToken cancellationToken = default)
        {
            EnsureNotDisposed();

            using HttpResponseMessage response = await _httpClient
                .GetAsync(NormalizeEndpoint(endpoint), cancellationToken)
                .ConfigureAwait(false);

            string content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                throw CreateApiException(response, content);
            }

            return _serializer.Deserialize<T>(content);
        }

        public async Task<IReadOnlyList<T>> GetCollectionAsync<T>(string endpoint, CancellationToken cancellationToken = default)
        {
            EnsureNotDisposed();

            using HttpResponseMessage response = await _httpClient
                .GetAsync(NormalizeEndpoint(endpoint), cancellationToken)
                .ConfigureAwait(false);

            string content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                throw CreateApiException(response, content);
            }

            return _serializer.DeserializeCollection<T>(content);
        }

        public void SetBearerToken(string token)
        {
            EnsureNotDisposed();

            if (string.IsNullOrWhiteSpace(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization = null;
            }
            else
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _httpClient.Dispose();
            _disposed = true;
        }

        private static HttpClient CreateHttpClient(ApiConfiguration configuration)
        {
            HttpMessageHandler handler = configuration.IgnoreCertificateErrors
                ? new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (
                        _,
                        _,
                        _,
                        _) => true
                }
                : new HttpClientHandler();

            var client = new HttpClient(handler, disposeHandler: true)
            {
                BaseAddress = configuration.BaseUri,
                Timeout = configuration.Timeout
            };

            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            return client;
        }

        private static string NormalizeEndpoint(string endpoint)
        {
            return string.IsNullOrWhiteSpace(endpoint) ? string.Empty : endpoint.Trim().TrimStart('/');
        }

        private static ApiException CreateApiException(HttpResponseMessage response, string responseContent)
        {
            HttpStatusCode statusCode = response.StatusCode;
            string endpoint = response.RequestMessage?.RequestUri?.ToString();
            string message = $"Request to '{endpoint}' failed with status {(int)statusCode} ({statusCode}).";

            return new ApiException(statusCode, message, responseContent, endpoint);
        }

        private void EnsureNotDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(ApiClient));
            }
        }
    }
}
