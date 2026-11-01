using Harjoitustyö.Models;
using Harjoitustyö.Repositories;

namespace Harjoitustyö.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _repository;
        private readonly IUserAuthenticationService _authenticationService;

        public UserService(IUserRepository repository, IUserAuthenticationService authenticationService)
        {
            _repository = repository;
            _authenticationService = authenticationService;
        }

        public async Task<IEnumerable<UserDTO>> GetUsersAsync()
        {
            IEnumerable<User> users = await _repository.GetUsersAsync();
            return users.Select(UserToDTO);
        }

        public async Task<UserDTO?> GetUserAsync(string userName)
        {
            User? user = await _repository.GetUserAsync(userName);
            return user == null ? null : UserToDTO(user);
        }

        public async Task<ServiceResult<UserDTO>> NewUserAsync(RegisterUserDTO dto)
        {
            if (await _repository.GetUserAsync(dto.UserName) != null)
            {
                return ServiceResult<UserDTO>.Fail(ServiceStatus.BadRequest, "Tunnus on jo käytössä");
            }

            User user = new User
            {
                UserName = dto.UserName,
                FirstName = dto.FirstName,
                LastName = dto.LastName
            };
            user = _authenticationService.CreateUserCredentials(user, dto.Password);

            User? created = await _repository.AddUserAsync(user);
            return created == null
                ? ServiceResult<UserDTO>.Fail(ServiceStatus.Failed)
                : ServiceResult<UserDTO>.Ok(UserToDTO(created));
        }

        public async Task<ServiceResult<UserDTO>> UpdateUserAsync(string userName, UpdateUserDTO dto)
        {
            User? user = await _repository.GetUserAsync(userName);
            if (user == null)
            {
                return ServiceResult<UserDTO>.Fail(ServiceStatus.NotFound);
            }

            if (dto.FirstName != null)
            {
                user.FirstName = dto.FirstName;
            }
            if (dto.LastName != null)
            {
                user.LastName = dto.LastName;
            }
            if (dto.Password != null)
            {
                user = _authenticationService.CreateUserCredentials(user, dto.Password);
            }

            return await _repository.UpdateUserAsync(user)
                ? ServiceResult<UserDTO>.Ok(UserToDTO(user))
                : ServiceResult<UserDTO>.Fail(ServiceStatus.Failed);
        }

        public async Task<ServiceResult<bool>> DeleteUserAsync(string userName)
        {
            User? user = await _repository.GetUserAsync(userName);
            if (user == null)
            {
                return ServiceResult<bool>.Fail(ServiceStatus.NotFound);
            }

            return await _repository.DeleteUserAsync(user)
                ? ServiceResult<bool>.Ok(true)
                : ServiceResult<bool>.Fail(ServiceStatus.Failed);
        }

        private static UserDTO UserToDTO(User user)
        {
            return new UserDTO
            {
                UserName = user.UserName,
                FirstName = user.FirstName,
                LastName = user.LastName,
                JoinDate = user.JoinDate,
                LastLogin = user.LastLogin
            };
        }
    }
}
