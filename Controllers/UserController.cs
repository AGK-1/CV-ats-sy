using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace cvAts.Controllers
{

    [Route("api/user")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class UserController : Controller
    {
        private readonly UserService _userService;

        public UserController(UserService userService)
        {
            _userService = userService;
        }

        [HttpGet("user")]
        public IActionResult Userpage()
        {
            var path = Path.Combine(
                Directory.GetCurrentDirectory(),
                "Protected/user.html");

            return PhysicalFile(path, "text/html");
        }


        [HttpGet("user-dashboard")]
        public async Task<IActionResult> UserDashboard()
        {
            var userIdClaim = User.FindFirst("userId")?.Value;

            if (userIdClaim == null)
                return Unauthorized();

            int userId = int.Parse(userIdClaim);

            var user = await _userService.GetUserByIdAsync(userId);

            return Ok(new
            {
                user.Id,
                user.Username,
                user.Email,
                user.IsEmailConfirmed,
                Role = user.Role.ToString()
            });


        }
    }
}

