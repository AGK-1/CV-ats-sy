using cvAts.Class;
using cvAts.DTO;
using cvAts.Services;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace cvAts.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TokenController : Controller
    {
        private readonly UserService _userService;
        private readonly TokenService _tokenService;

        public TokenController(UserService userService, TokenService tokenService)
        {
            _userService = userService;
            _tokenService = tokenService;
        }


        [HttpGet("get-tokens")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,Roles = "Admin")]
        public async Task<IActionResult> getAllTokens()
        {
            var tokens = await _tokenService.GetYourTokens();
            return Ok(tokens);
        }


        [HttpGet("get-token-with-id-admin")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")]
        public async Task<IActionResult> getTokens(int userId)
        {
            var yourToken = await _tokenService.GetYourOwnToken(userId);
            return Ok(yourToken);
        }


        [HttpGet("get-only-your-tokens")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> GetMyTokens()
        {
            var userIdClaim = User.FindFirst("userId")?.Value;

            if (userIdClaim == null)
                return Unauthorized("You not autorized!");

            int userId = int.Parse(userIdClaim);

            var yourToken = await _tokenService.GetYourOwnToken(userId);
            return Ok(yourToken);
        }

        [HttpPost("add-tokens")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> PostNewToken(int token)
        {
            var userIdClaim = User.FindFirst("userId")?.Value;

            if (userIdClaim == null)
                return Unauthorized();

            int userId = int.Parse(userIdClaim);

            await _tokenService.AddTokens(userId, token);

            return Ok("Token added successfully.");
        }

        [HttpPut("update-tokens")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> UpdateTokens(int token)
        {
            var userIdClaim = User.FindFirst("userId")?.Value;

            if (userIdClaim == null)
                return Unauthorized();

            int userId = int.Parse(userIdClaim);

            await _tokenService.UpdateTokens(userId, token);
            return Ok("Token updated successfully.");
        }


        [HttpPut("update-token-admin")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,Roles = "Admin")]
        public async Task<IActionResult> UpdateTokensAdmin([FromBody] NewTokenAdminDTO dto)
        {
            int userId = dto.userId;
            int token = dto.Token;
            await _tokenService.UpdateTokens(userId, token);
            return Ok("Token updated successfully.");
        }

        [HttpPost("add-token-admin")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")]
        public async Task<IActionResult> AddTokensAdmin([FromBody] NewTokenAdminDTO dto)
        {
            int userId = dto.userId;
            int token = dto.Token;
            await _tokenService.AddTokens(userId, token);
            return Ok("Token added successfully.");
        }

        [HttpPost("add-token-admin-email")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")]
        public async Task<IActionResult> AddTokenEmailAdmin([FromBody] TokenAdminEmailDTO dto)
        {
            int token = dto.Token;
            await _tokenService.AddTokenAdminEmail(token, dto.Email);
            return Ok("Token added successfully.");
        }

        [HttpPut("update-token-admin-email")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")]
        public async Task<IActionResult> UpdateTokensAdminEmail([FromBody] TokenAdminEmailDTO dto)
        {
            string email = dto.Email;
            int token = dto.Token;
            await _tokenService.UpdateTokenAdminEmail(email, token);
            return Ok("Token updated successfully.");
        }


        [HttpDelete("delete-your-one-token")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> DeleteOneToken()
        {
            var userIdClaim = User.FindFirst("userId")?.Value;

            if (userIdClaim == null)
                return Unauthorized();

            int userId = int.Parse(userIdClaim);
            await _tokenService.deleteOneToken(userId);
            return Ok("One token deleted");
        }

        [HttpGet("check")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "SuperAdmin")]
        public string cehAllTokens()
        {
           
            return ("Qasimzade");
        }

        [HttpGet("check-two")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")]
        public string checktwp()
        {

            return ("Qasimzade 2 ");
        }
    }
}
