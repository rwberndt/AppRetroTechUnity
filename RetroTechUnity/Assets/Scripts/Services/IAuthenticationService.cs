using RetroTech;

namespace RetroTech.Services
{
    /// <summary>
    /// Abstraction over the authentication workflow used by the RetroTech prototype.  The
    /// implementation included in the project stores credentials locally using PlayerPrefs so
    /// it can run fully offline while mimicking a cloud backed flow.
    /// </summary>
    public interface IAuthenticationService
    {
        bool TryRegister(string username, string password, out string errorMessage);
        bool TrySignIn(string username, string password, out UserProfile profile, out string errorMessage);
        bool TryAutoSignIn(out UserProfile profile);
        void SignOut();
    }
}
