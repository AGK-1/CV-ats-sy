using System;
using cvAts.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static Org.BouncyCastle.Crypto.Engines.SM2Engine;

namespace cvAts.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ForgotPassword : ControllerBase
    {

        String[] st = { "Alyesa", "Alqalim", "Alim", "Albali" };
        private readonly AppDbContext _context;
        private readonly JwtService _jwt;
        private readonly EmailService _emailService;
        Random random = new Random();
        private readonly TempStorageService _tempStorageService;
        //private String emailCheck;

        public ForgotPassword(AppDbContext context, JwtService jwt, EmailService emailService, TempStorageService temp)
        {
            _context = context;
            _jwt = jwt;
            _emailService = emailService;
            _tempStorageService = temp;
        }

 

        [HttpPost("send-code")]
        public async Task<IActionResult> ForgotPass(string email)
        {
            if (email == null) { return BadRequest("This email not found"); }
            var randomNumber = random.Next(100000, 999999).ToString(); // от 1000 до 9999

            _tempStorageService.SetTempValue("confirmEmail", email, 300);
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

            return Ok("code send your email check it!");
          
        }

        [HttpPost("confirm-code")]
        public async Task<IActionResult> confirm_code(String code)
        {
            var checkCode = _tempStorageService.GetTempValue("confirmCode");
            if (checkCode == null)
            {
                return BadRequest("OTP code not exists r expired! ");
            }

            if (checkCode == code)
            {
                return Ok("Now you can change your password! ");
            }
            else
            {
                return BadRequest("Wrong OTP code");
            }
                  
        }


        [HttpPost("change-password")]
        public async Task<IActionResult> VerifyOtpCode(string code, string codeV)
        {
            var emailCheck = _tempStorageService.GetTempValue("confirmEmail");

            if (emailCheck == null)
            {
                return BadRequest("Wrong email or code date expired! ");
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == emailCheck);
            if (user == null)
                return BadRequest("User not found!");

           
            if (code == codeV)
                user.Password = BCrypt.Net.BCrypt.HashPassword(codeV);
            //user.Password = code;
            //user.IsEmailConfirmed = true;
            await _context.SaveChangesAsync();

            return Ok("Password changed! ");
        }

        [HttpGet("Email Check7")]
        public String getEMail()
        {
            return "Email kestrelc " + _tempStorageService.GetTempValue("confirmEmail");
        }
        
    }
}
