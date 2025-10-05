namespace RetroTech
{
    /// <summary>
    /// Lightweight representation of the authenticated RetroTech user.  Only stores the
    /// information that the UI may need to render greetings or personalise the experience.
    /// </summary>
    [System.Serializable]
    public class UserProfile
    {
        public string DisplayName { get; }
        public string Email { get; }

        public UserProfile(string displayName, string email)
        {
            DisplayName = displayName;
            Email = email;
        }

        public override string ToString()
        {
            return string.IsNullOrEmpty(DisplayName) ? Email : $"{DisplayName} ({Email})";
        }
    }
}
