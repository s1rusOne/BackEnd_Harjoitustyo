using Harjoitustyö.Models;

namespace Harjoitustyö.Services
{
    public interface IUserService
    {
        Task<IEnumerable<UserDTO>> GetUsersAsync();
        Task<UserDTO?> GetUserAsync(string userName);
        Task<ServiceResult<UserDTO>> NewUserAsync(RegisterUserDTO user);
        Task<ServiceResult<UserDTO>> UpdateUserAsync(string userName, UpdateUserDTO user);
        Task<ServiceResult<bool>> DeleteUserAsync(string userName);
    }
}
