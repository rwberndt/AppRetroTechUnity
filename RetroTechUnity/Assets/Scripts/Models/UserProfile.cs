namespace RetroTech
{
    /// <summary>
    /// Lightweight representation of the authenticated RetroTech user.  Only stores the
    /// information that the UI may need to render greetings or personalise the experience.
    /// </summary>
    [System.Serializable]
    public class UserProfile
    {
        public string Username { get; }
        public string AccessToken { get; }

        public UserProfile(string username, string accessToken = null)
        {
            Username = username;
            AccessToken = accessToken ?? string.Empty;
        }

        public override string ToString()
        {
            return Username ?? string.Empty;
        }
    }
}
