using System;
using System.Net;

namespace RetroTech.Services
{
    /// <summary>
    /// Represents an error returned by the RetroTech API.
    /// </summary>
    public class ApiException : Exception
    {
        public HttpStatusCode StatusCode { get; }
        public string ResponseContent { get; }
        public string Endpoint { get; }

        public ApiException(
            HttpStatusCode statusCode,
            string message,
            string responseContent = null,
            string endpoint = null,
            Exception innerException = null)
            : base(message, innerException)
        {
            StatusCode = statusCode;
            ResponseContent = responseContent ?? string.Empty;
            Endpoint = endpoint ?? string.Empty;
        }
    }
}
