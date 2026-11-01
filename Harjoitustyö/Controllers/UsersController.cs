using Harjoitustyö.Models;
using Harjoitustyö.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Harjoitustyö.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserDTO>>> GetUsers()
        {
            return Ok(await _userService.GetUsersAsync());
        }

        [HttpGet("me")]
        public async Task<ActionResult<UserDTO>> GetMe()
        {
            return await GetUser(User.Identity!.Name!);
        }

        [HttpGet("{userName}")]
        public async Task<ActionResult<UserDTO>> GetUser(string userName)
        {
            UserDTO? user = await _userService.GetUserAsync(userName);
            if (user == null)
            {
                return NotFound();
            }
            return user;
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<ActionResult<UserDTO>> PostUser(RegisterUserDTO user)
        {
            var result = await _userService.NewUserAsync(user);
            if (result.Status != ServiceStatus.Success)
            {
                return this.ErrorResult(result);
            }
            return CreatedAtAction(nameof(GetUser), new { userName = result.Value!.UserName }, result.Value);
        }

        [HttpPut("{userName}")]
        public async Task<ActionResult<UserDTO>> PutUser(string userName, UpdateUserDTO user)
        {
            if (!IsCurrentUser(userName))
            {
                return StatusCode(StatusCodes.Status403Forbidden, "Vain omaa profiilia voi muokata");
            }

            var result = await _userService.UpdateUserAsync(userName, user);
            if (result.Status != ServiceStatus.Success)
            {
                return this.ErrorResult(result);
            }
            return result.Value!;
        }

        [HttpDelete("{userName}")]
        public async Task<IActionResult> DeleteUser(string userName)
        {
            if (!IsCurrentUser(userName))
            {
                return StatusCode(StatusCodes.Status403Forbidden, "Vain oman profiilin voi poistaa");
            }

            var result = await _userService.DeleteUserAsync(userName);
            if (result.Status != ServiceStatus.Success)
            {
                return this.ErrorResult(result);
            }
            return NoContent();
        }

        private bool IsCurrentUser(string userName)
        {
            return string.Equals(User.Identity?.Name, userName, StringComparison.Ordinal);
        }
    }
}
