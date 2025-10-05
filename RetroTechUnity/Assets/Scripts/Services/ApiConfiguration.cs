using System;
using UnityEngine;

namespace RetroTech.Services
{
    /// <summary>
    /// Represents the configuration required to communicate with the RetroTech API. The data
    /// is loaded from <c>Resources/api-config.json</c> so it can be edited without recompiling
    /// the project.
    /// </summary>
    public class ApiConfiguration
    {
        private const string ResourcePath = "api-config";
        private const string DefaultBaseUrl = "https://localhost:5001/api";

        public Uri BaseUri { get; }
        public TimeSpan Timeout { get; }
        public bool IgnoreCertificateErrors { get; }
        public string CategoriesEndpoint { get; }
        public string PiecesEndpoint { get; }
        public string QuizEndpoint { get; }
        public string AuthLoginEndpoint { get; }
        public string AuthRegisterEndpoint { get; }

        private ApiConfiguration(ApiConfigurationData data)
        {
            string baseUrl = string.IsNullOrWhiteSpace(data.baseUrl) ? DefaultBaseUrl : data.baseUrl;
            baseUrl = EnsureTrailingSlash(baseUrl.Trim());

            if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var baseUri))
            {
                Debug.LogWarning($"Invalid API base URL '{data.baseUrl}'. Falling back to '{DefaultBaseUrl}'.");
                baseUri = new Uri(EnsureTrailingSlash(DefaultBaseUrl));
            }

            BaseUri = baseUri;
            Timeout = TimeSpan.FromSeconds(data.timeoutSeconds > 0 ? data.timeoutSeconds : 30f);
            IgnoreCertificateErrors = data.ignoreCertificateErrors;

            CategoriesEndpoint = NormalizeEndpoint(data.endpoints?.categories, "categories");
            PiecesEndpoint = NormalizeEndpoint(data.endpoints?.pieces, "pieces");
            QuizEndpoint = NormalizeEndpoint(data.endpoints?.quiz, "quiz");
            AuthLoginEndpoint = NormalizeEndpoint(data.endpoints?.authLogin, "auth/login");
            AuthRegisterEndpoint = NormalizeEndpoint(data.endpoints?.authRegister, "auth/register");
        }

        public static ApiConfiguration Load()
        {
            TextAsset configAsset = Resources.Load<TextAsset>(ResourcePath);
            if (configAsset == null)
            {
                Debug.LogWarning("API configuration resource 'api-config' not found. Using default values.");
                return new ApiConfiguration(new ApiConfigurationData());
            }

            try
            {
                var data = JsonUtility.FromJson<ApiConfigurationData>(configAsset.text);
                if (data == null)
                {
                    Debug.LogWarning("API configuration file was empty. Using default values.");
                    data = new ApiConfigurationData();
                }

                return new ApiConfiguration(data);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Unable to parse API configuration. Using default values. {ex}");
                return new ApiConfiguration(new ApiConfigurationData());
            }
        }

        private static string EnsureTrailingSlash(string value)
        {
            return value.EndsWith("/") ? value : value + "/";
        }

        private static string NormalizeEndpoint(string value, string fallback)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return fallback;
            }

            return value.Trim().Trim('/');
        }

        [Serializable]
        private class ApiConfigurationData
        {
            public string baseUrl = DefaultBaseUrl;
            public float timeoutSeconds = 30f;
            public bool ignoreCertificateErrors = true;
            public EndpointConfiguration endpoints = new EndpointConfiguration();
        }

        [Serializable]
        private class EndpointConfiguration
        {
            public string categories = "categories";
            public string pieces = "pieces";
            public string quiz = "quiz";
            public string authLogin = "auth/login";
            public string authRegister = "auth/register";
        }
    }
}
