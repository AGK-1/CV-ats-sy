using cvAts.DTO;
using cvAts.Services;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OpenAI.Responses;

namespace cvAts.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdminController : ControllerBase
    {

        private readonly AdminService _adminService;

        public AdminController(AdminService adminService)
        {
            _adminService = adminService;
        }

        [HttpGet("get")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")]
        public async Task<IActionResult> YourTest()
        {
            var tokens = "Tokens";
            return Ok(tokens);
        }



        [HttpPost("login-as-user")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")]
        public async Task<IActionResult> LoginAsUser(int targetUser)
        {
            var adminId = int.Parse(User.FindFirst("userId")!.Value);

            var token = await _adminService.LoginAsUser(adminId, targetUser);

            return Ok(new { token });
        }

        [HttpPost("stop-impersonation")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> StopImpersonation()
        {
            var impersonatedBy = User.FindFirst("impersonatedBy")?.Value;

            if (string.IsNullOrEmpty(impersonatedBy))
                return BadRequest("You are not impersonating a user.");

            var token = await _adminService.StopImpersonation(int.Parse(impersonatedBy));

            return Ok(new { token });
        }


        [HttpGet("get-all-users")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> GetUsers()
        {
            var userIdClaim = User.FindFirst("userId")?.Value;

            if (userIdClaim == null)
                return Unauthorized("You not autorized!");

            int userId = int.Parse(userIdClaim);
            var users = await _adminService.GetAllUsers(userId);
            return Ok(users);
        }

        [HttpPost("assign-admin-role")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> AssingAdminRole(string email)
        {
            var userIdClaim = User.FindFirst("userId")?.Value;

            if (userIdClaim == null)
                return Unauthorized("You not autorized!");

            int userId = int.Parse(userIdClaim);
            var newAdmin = await _adminService.AssignAdminRole(userId, email);
            return Ok("Now in role admin");
        }

        [HttpPost("assign-user-role")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> AssignUserRole(string email)
        {
            var userIdClaim = User.FindFirst("userId")?.Value;

            if (userIdClaim == null)
                return Unauthorized("You not autorized!");

            int userId = int.Parse(userIdClaim);
            var newAdmin = await _adminService.AssignUserRole(userId, email);
            return Ok("Now in role user");
        }

        [HttpDelete("delete-user")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var userIdClaim = User.FindFirst("userId")?.Value;

            if (userIdClaim == null)
                return Unauthorized("You not autorized!");

            int userId = int.Parse(userIdClaim);

            await _adminService.DeleteUser(id);
            return Ok("User deleted");
        }


        [HttpDelete("delete-user-with-email")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> DeleteUserWithEmail([FromBody] string email)
        {

            var userIdClaim = User.FindFirst("userId")?.Value;

            if (userIdClaim == null)
                return Unauthorized("You not autorized!");

            int userId = int.Parse(userIdClaim);
            await _adminService.DeleteUserWithEmail(email);
            return Ok("User Deleted");
        }

        [HttpGet("get-all-users-history")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> GetAllUsersHistory()
        {
            var list = await _adminService.GetUserHistory();
            return Ok(list);
        }


    }
}
