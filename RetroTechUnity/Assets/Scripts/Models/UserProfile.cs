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

        public UserProfile(string username)
        {
            Username = username;
        }

        public override string ToString()
        {
            return Username ?? string.Empty;
        }
    }
}
