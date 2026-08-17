using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using cvAts.Class;
using cvAts.DTO;
using cvAts.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Org.BouncyCastle.Asn1.Ocsp;
using static Org.BouncyCastle.Crypto.Engines.SM2Engine;
using static Org.BouncyCastle.Math.EC.ECCurve;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.Cookies;
using Npgsql;

using System.Net.Http;
using System.Text.Json;

namespace cvAts.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly JwtService _jwt;
        private readonly EmailService _emailService;
        private readonly TempStorageService _tempStorageService;
        private readonly AuditService _auditService;
        private readonly IConfiguration _config;


        public AuthController(AppDbContext context, JwtService jwt, EmailService emailService, TempStorageService temp, AuditService auditService, IConfiguration config)
        {

            _context = context;
            _jwt = jwt;
            _emailService = emailService;
            _tempStorageService = temp;
            _auditService = auditService;
            _config = config;
        }
        Random random = new Random();

        // Случайное целое число от 0 до Int32.MaxValue
        String randomNumber;
        int randomValue;


        [HttpGet("check-db-pgadmin")]
        public async Task<IActionResult> TestConnection()
        {
            var connString = _config.GetConnectionString("DefaultConnection");

            await using var conn = new NpgsqlConnection(connString);

            try
            {
                await conn.OpenAsync();
                return Ok("Connected to PostgreSQL!");
            }
            catch (Exception ex)
            {
                return BadRequest("Error: " + ex.Message);
            }
        }

        // GET: /api/Auth/google-response
        [HttpGet("google-response")]
        public async Task<IActionResult> GoogleResponse()
        {
            var result = await HttpContext.AuthenticateAsync();
            if (!result.Succeeded)
                return BadRequest("Ошибка входа через Google");

            var claims = result.Principal.Identities.FirstOrDefault()?.Claims;
            var email = claims?.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var name = claims?.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;

            //var user = new User { Id = 1, Username = name ?? email ?? "Unknown" };
            if (string.IsNullOrEmpty(email))
                return BadRequest("Не удалось получить email из Google");

            // Проверяем, есть ли пользователь в базе
            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

            User user;
            if (existingUser == null)
            {
                // Новый пользователь
                user = new User
                {
                    Username = name ?? email,
                    Email = email,
                    Password = BCrypt.Net.BCrypt.HashPassword("google1225" + email),
                    IsEmailConfirmed = true,
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();
            }
            else
            {
                user = existingUser;
            }
            var token = _jwt.GenerateToken(user);





            return Ok(new { token, email, name });
        }


        [HttpGet("protected")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public IActionResult Protected()
        {
            return Ok(new { message = "You are authorized!", user = User.Identity?.Name });
        }



        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDto dto)
        {
            if (await _context.Users.AnyAsync(u => u.Username == dto.Username))
                return BadRequest("Username already exits");
            if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
                return BadRequest("User with that email already exits");

            var user = new User
            {
                Username = dto.Username,
                Email = dto.Email,
                Password = BCrypt.Net.BCrypt.HashPassword(dto.Password)
            };
            var code = new Random().Next(100000, 999999).ToString();
            _tempStorageService.SetTempValue($"confirmCode:{user.Email}", code, 250);
            //_tempStorageService.SetTempValue("confirmCode", code, 250);

            //_tempStorageService.SetPermanentValue("confirmEmail", user.Email);

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var body = $@"
    <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
        <h2 style='color: #333;'>Welcome!</h2>
        <p>To complete registration, use the following code:</p>
        <div style='background-color: #f5f5f5; padding: 15px; text-align: center; margin: 20px 0;'>
            <span style='font-size: 32px; font-weight: bold; letter-spacing: 5px; color: #d35400;'>{code}</span>
        </div>
        <p style='color: #666; font-size: 12px;'>
            If you did not request registration, please ignore this email.
        </p>
    </div>";

            await _emailService.SendEmailAsync(dto.Email, "Подтверждение регистрации",
                body);

            return Ok("The letter with the code has been sent.!");
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
            if (user == null)
                return Unauthorized("User not found");

            // Отладочная информация
            Console.WriteLine($"Input password: {dto.Password}");
            Console.WriteLine($"Stored password: {user.Password}");
            Console.WriteLine($"Is BCrypt hash: {user.Password?.StartsWith("$2")}");

            var isPasswordCorrect = BCrypt.Net.BCrypt.Verify(dto.Password, user.Password);
            Console.WriteLine($"Password verification: {isPasswordCorrect}");
            //if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.Password))
            //    return Unauthorized("Wrong password! ");
            if (!isPasswordCorrect)
                return Unauthorized("Wrong password!");
            _tempStorageService.SetPermanentValue("confirmEmail", user.Email);

            await _auditService.LogLoginAsync(
                   user.Id,
                   true,
                  "Successful login"
            );

            var token = "Bearer " + _jwt.GenerateToken(user);
            // var token = _jwt.GenerateToken(user);
            return Ok(new { token });
        }

        [HttpPost("verify")]
        public async Task<IActionResult> VerifyEmail(string email, string code)
        {
            var storedCode = _tempStorageService.GetTempValue($"confirmCode:{email}");
            if (storedCode == null)
                return BadRequest("The code has expired or was not found.");

            if (storedCode != code)
                return BadRequest("Wrong code");


            //var storedCode = _tempStorageService.GetTempValue("confirmCode");
            ////_tempStorageService.GetTempValue("confirmCode");
            //var storedEmail = _tempStorageService.GetPermanentValue("confirmEmail");

            //if (storedCode == null)
            //    return BadRequest("The code has expired or was not found.");
            //if (storedCode != code)
            //    return BadRequest("Wrong code");
            //if (email != storedEmail)
            //    return BadRequest("Email not existing or already confirmed!");
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
                return BadRequest("User not found! ");
            if (user.IsEmailConfirmed == true)
                return BadRequest("User already is confirmed! ");
            user.IsEmailConfirmed = true;
            await _context.SaveChangesAsync();
            Console.WriteLine($"Email = {email}");
            Console.WriteLine($"StoredCode = {storedCode}");
            Console.WriteLine($"InputCode = {code}");
            return Ok("Email successfully confirmed!");
        }

        [HttpPost("send-confirmation-code-again")]
        public async Task<IActionResult> SendConfirmationEmail(string email)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
                return NotFound("User not found in database!");
            if (user.IsEmailConfirmed)
            {
                return BadRequest("This email already confirmed");
            }
            var storedEmail = _tempStorageService.GetPermanentValue("confirmEmail");
            if (storedEmail != email)
                return BadRequest("This email not registered or already confirmed!");

            var randomNumber = random.Next(100000, 999999).ToString(); // от 1000 до 9999
            _tempStorageService.SetTempValue("confirmCode", randomNumber, 300);

            var body = $@"
    <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
        <h2 style='color: #333;'>Добро пожаловать!</h2>
        <p>Для завершения регистрации используйте следующий код:</p>
        <div style='background-color: #f5f5f5; padding: 15px; text-align: center; margin: 20px 0;'>
            <span style='font-size: 32px; font-weight: bold; letter-spacing: 5px; color: #d35400;'>{_tempStorageService.GetTempValue("confirmCode")}</span>
        </div>
        <p style='color: #666; font-size: 12px;'>
            Если вы не запрашивали регистрацию, проигнорируйте это письмо.
        </p>
    </div>";
            await _emailService.SendEmailAsync(email, "Подтверждение регистрации",
               body);

            return Ok("Письмо отправлено!");
        }


        [HttpGet("login-with-google")]
        public async Task<IActionResult> LoginWithGoogle()
        {
            var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            if (!result.Succeeded)
                return BadRequest("Google login failed");

            // Пример: получить email и имя пользователя
            var claims = result.Principal.Identities
                .FirstOrDefault()?.Claims.Select(c => new { c.Type, c.Value });

            // Можно здесь создать или найти пользователя в базе
            // и установить cookie через SignInAsync, если нужно

            return Ok(claims); // Для теста возвращаем JSON
            //var properties = new AuthenticationProperties
            //{
            //    RedirectUri = Url.Action(nameof(GoogleResponse))
            //};

            //return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }



        [HttpGet("admin")]
        public string testapi()
        {
            var code = _tempStorageService.GetTempValue("confirmCode");
            var eml = _tempStorageService.GetPermanentValue("confirmEmail");
            return "My generated code -" + code + " My email " + eml;
        }

        [HttpGet("JWT-CHECK")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> Getjwt()
        {
            var username = User.FindFirst("username")?.Value;
            var email = User.FindFirst("email")?.Value;

            if (string.IsNullOrEmpty(username) && string.IsNullOrEmpty(email))
                return Unauthorized("Token does not contain valid claims");

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == username || u.Email == email);

            if (user == null)
                return NotFound("User not found in database");

            return Ok(new
            {
                message = "JWT token is valid",
                user
            });
        }

        [HttpGet("user-token")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public IActionResult Checkuser()
        {
            return Ok("Success log");
        }

        [AllowAnonymous]
        [HttpGet("ping")]
        public IActionResult Ping()
        {
            return Ok("pong");
        }
        [AllowAnonymous]
        [HttpGet("debug-auth")]
        public IActionResult DebugAuth()
        {
            return Ok(User.Identity?.IsAuthenticated);
        }
    }
}
