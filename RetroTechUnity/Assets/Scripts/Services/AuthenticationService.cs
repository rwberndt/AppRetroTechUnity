using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RetroTech;
using UnityEngine;

namespace RetroTech.Services
{
    /// <summary>
    /// Authentication service that communicates with the RetroTech API to register users and
    /// exchange credentials for JWT tokens. Successful authentications are cached locally so
    /// the app can attempt an automatic sign-in on startup.
    /// </summary>
    public class AuthenticationService : IAuthenticationService, IDisposable
    {
        private const string TokenKey = "RetroTech.Auth.Token";
        private const string UsernameKey = "RetroTech.Auth.Username";

        [Serializable]
        private class AuthRequest
        {
            public string username;
            public string password;
        }

        [Serializable]
        private class AuthResponse
        {
            public string token;
            public string accessToken;
            public string jwt;
            public string bearerToken;
            public string username;
            public string userName;
        }

        [Serializable]
        private class ErrorResponse
        {
            public string message;
            public string error;
            public string detail;
            public string title;
        }

        private readonly HttpClient _httpClient;
        private readonly string _loginEndpoint;
        private readonly string _registerEndpoint;
        private bool _disposed;

        public AuthenticationService(ApiConfiguration configuration)
        {
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));

            _httpClient = CreateHttpClient(configuration);
            _loginEndpoint = NormalizeEndpoint(configuration.AuthLoginEndpoint);
            _registerEndpoint = NormalizeEndpoint(configuration.AuthRegisterEndpoint);
        }

        public async Task<AuthenticationResult> RegisterAsync(string username, string password, CancellationToken cancellationToken = default)
        {
            string validationError = ValidateCredentials(username, password);
            if (!string.IsNullOrEmpty(validationError))
            {
                return AuthenticationResult.Failure(validationError);
            }

            return await SendAuthRequestAsync(_registerEndpoint, NormalizeUsername(username), password, cancellationToken);
        }

        public async Task<AuthenticationResult> SignInAsync(string username, string password, CancellationToken cancellationToken = default)
        {
            string validationError = ValidateCredentials(username, password, allowShortPassword: true);
            if (!string.IsNullOrEmpty(validationError))
            {
                return AuthenticationResult.Failure(validationError);
            }

            return await SendAuthRequestAsync(_loginEndpoint, NormalizeUsername(username), password, cancellationToken);
        }

        public bool TryAutoSignIn(out UserProfile profile)
        {
            string cachedUsername = PlayerPrefs.GetString(UsernameKey, string.Empty);
            string cachedToken = PlayerPrefs.GetString(TokenKey, string.Empty);

            if (string.IsNullOrWhiteSpace(cachedUsername) || string.IsNullOrWhiteSpace(cachedToken))
            {
                profile = null;
                return false;
            }

            profile = new UserProfile(cachedUsername, cachedToken);
            return true;
        }

        public void SignOut()
        {
            ClearSession();
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

        private async Task<AuthenticationResult> SendAuthRequestAsync(string endpoint, string username, string password, CancellationToken cancellationToken)
        {
            EnsureNotDisposed();

            if (string.IsNullOrEmpty(endpoint))
            {
                return AuthenticationResult.Failure("Endpoint de autenticação não configurado.");
            }

            var requestPayload = new AuthRequest
            {
                username = username,
                password = password
            };

            string json = JsonUtility.ToJson(requestPayload);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                using HttpResponseMessage response = await _httpClient
                    .PostAsync(endpoint, content, cancellationToken);

                string responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var profile = ParseProfile(responseContent, username);
                    if (profile == null || string.IsNullOrWhiteSpace(profile.AccessToken))
                    {
                        return AuthenticationResult.Failure("Resposta inválida do servidor de autenticação.");
                    }

                    CacheSession(profile);
                    return AuthenticationResult.Success(profile);
                }

                string errorMessage = ExtractErrorMessage(responseContent, response.ReasonPhrase);
                return AuthenticationResult.Failure(errorMessage);
            }
            catch (OperationCanceledException)
            {
                return AuthenticationResult.Failure("A solicitação foi cancelada. Tente novamente.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Authentication request failed: {ex}");
                return AuthenticationResult.Failure("Não foi possível se conectar ao servidor. Verifique sua conexão e tente novamente.");
            }
        }

        private static UserProfile ParseProfile(string responseContent, string fallbackUsername)
        {
            string username = fallbackUsername;
            string token = string.Empty;

            if (!string.IsNullOrWhiteSpace(responseContent))
            {
                try
                {
                    var parsed = JsonUtility.FromJson<AuthResponse>(responseContent);
                    if (parsed != null)
                    {
                        username = !string.IsNullOrWhiteSpace(parsed.username)
                            ? parsed.username.Trim()
                            : !string.IsNullOrWhiteSpace(parsed.userName)
                                ? parsed.userName.Trim()
                                : fallbackUsername;

                        token = CoalesceToken(parsed.token, parsed.accessToken, parsed.jwt, parsed.bearerToken);
                    }
                }
                catch (ArgumentException)
                {
                    // If the payload is not a JSON object we fall back to additional heuristics below.
                }

                if (string.IsNullOrWhiteSpace(token))
                {
                    string trimmed = responseContent.Trim().Trim('\"');
                    if (!trimmed.StartsWith("{") && !trimmed.StartsWith("["))
                    {
                        token = trimmed;
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(token))
            {
                return null;
            }

            return new UserProfile(username, token);
        }

        private static string CoalesceToken(params string[] candidates)
        {
            if (candidates == null)
            {
                return string.Empty;
            }

            foreach (var candidate in candidates)
            {
                if (!string.IsNullOrWhiteSpace(candidate))
                {
                    return candidate.Trim();
                }
            }

            return string.Empty;
        }

        private static string ExtractErrorMessage(string responseContent, string fallback)
        {
            if (!string.IsNullOrWhiteSpace(responseContent))
            {
                try
                {
                    var error = JsonUtility.FromJson<ErrorResponse>(responseContent);
                    if (error != null)
                    {
                        if (!string.IsNullOrWhiteSpace(error.message))
                            return error.message.Trim();
                        if (!string.IsNullOrWhiteSpace(error.error))
                            return error.error.Trim();
                        if (!string.IsNullOrWhiteSpace(error.detail))
                            return error.detail.Trim();
                        if (!string.IsNullOrWhiteSpace(error.title))
                            return error.title.Trim();
                    }
                }
                catch (ArgumentException)
                {
                    // ignored - we'll fall back to the raw string.
                }

                string plain = responseContent.Trim().Trim('\"');
                if (!string.IsNullOrWhiteSpace(plain))
                {
                    return plain;
                }
            }

            return string.IsNullOrWhiteSpace(fallback)
                ? "Falha ao processar a solicitação de autenticação."
                : fallback;
        }

        private static string ValidateCredentials(string username, string password, bool allowShortPassword = false)
        {
            string normalizedUsername = NormalizeUsername(username);
            string safePassword = password ?? string.Empty;

            if (string.IsNullOrWhiteSpace(normalizedUsername))
            {
                return "Informe um nome de usuário.";
            }

            if (normalizedUsername.Length < 3)
            {
                return "O nome de usuário deve ter pelo menos 3 caracteres.";
            }

            if (string.IsNullOrEmpty(safePassword))
            {
                return "Informe sua senha.";
            }

            if (!allowShortPassword && safePassword.Length < 6)
            {
                return "A senha deve ter pelo menos 6 caracteres.";
            }

            return string.Empty;
        }

        private static string NormalizeUsername(string username)
        {
            return string.IsNullOrWhiteSpace(username) ? string.Empty : username.Trim();
        }

        private static string NormalizeEndpoint(string endpoint)
        {
            return string.IsNullOrWhiteSpace(endpoint) ? string.Empty : endpoint.Trim().Trim('/');
        }

        private static HttpClient CreateHttpClient(ApiConfiguration configuration)
        {
            HttpMessageHandler handler = configuration.IgnoreCertificateErrors
                ? new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (_, _, _, _) => true
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

        private static void CacheSession(UserProfile profile)
        {
            PlayerPrefs.SetString(UsernameKey, profile?.Username ?? string.Empty);
            PlayerPrefs.SetString(TokenKey, profile?.AccessToken ?? string.Empty);
            PlayerPrefs.Save();
        }

        private static void ClearSession()
        {
            PlayerPrefs.DeleteKey(UsernameKey);
            PlayerPrefs.DeleteKey(TokenKey);
            PlayerPrefs.Save();
        }

        private void EnsureNotDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(AuthenticationService));
            }
        }
    }
}
