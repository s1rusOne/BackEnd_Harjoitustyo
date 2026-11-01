using Harjoitustyö.Models;

namespace Harjoitustyö.Services
{
    public interface IUserAuthenticationService
    {
        Task<User?> Authenticate(string userName, string password);
        User CreateUserCredentials(User user, string password);
    }
}
