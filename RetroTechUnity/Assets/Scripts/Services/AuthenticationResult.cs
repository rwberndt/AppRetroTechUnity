using RetroTech;

namespace RetroTech.Services
{
    /// <summary>
    /// Represents the outcome of an authentication attempt, encapsulating both the
    /// authenticated profile (when successful) and any error message that should be
    /// surfaced to the user on failure.
    /// </summary>
    public readonly struct AuthenticationResult
    {
        public bool Succeeded { get; }
        public UserProfile Profile { get; }
        public string ErrorMessage { get; }

        private AuthenticationResult(bool succeeded, UserProfile profile, string errorMessage)
        {
            Succeeded = succeeded;
            Profile = profile;
            ErrorMessage = errorMessage ?? string.Empty;
        }

        public static AuthenticationResult Success(UserProfile profile)
        {
            return new AuthenticationResult(true, profile, string.Empty);
        }

        public static AuthenticationResult Failure(string errorMessage)
        {
            return new AuthenticationResult(false, null, errorMessage);
        }
    }
}
