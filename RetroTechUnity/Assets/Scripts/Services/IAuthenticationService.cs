using System.Threading;
using System.Threading.Tasks;
using RetroTech;

namespace RetroTech.Services
{
    /// <summary>
    /// Abstraction over the authentication workflow used by the RetroTech prototype.  The
    /// implementation shipped with the project delegates to the remote API in order to
    /// exchange credentials for a JWT that is later reused across the app.
    /// </summary>
    public interface IAuthenticationService
    {
        Task<AuthenticationResult> RegisterAsync(string username, string password, CancellationToken cancellationToken = default);
        Task<AuthenticationResult> SignInAsync(string username, string password, CancellationToken cancellationToken = default);
        bool TryAutoSignIn(out UserProfile profile);
        void SignOut();
    }
}
