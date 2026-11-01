using Harjoitustyö.Models;

namespace Harjoitustyö.Repositories
{
    public interface IUserRepository
    {
        Task<IEnumerable<User>> GetUsersAsync();
        Task<User?> GetUserAsync(string userName);
        Task<User?> GetUserAsync(long id);
        Task<User?> AddUserAsync(User user);
        Task<bool> UpdateUserAsync(User user);
        Task<bool> DeleteUserAsync(User user);
    }
}
