using cvAts.Class;
using cvAts.DTO;
using cvAts.Services;
using DocumentFormat.OpenXml.Vml;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Org.BouncyCastle.Asn1.Ocsp;
using Org.BouncyCastle.Asn1.Pkcs;
using Tesseract;

namespace cvAts.Controllers
{


    [Route("api/[controller]")]
    [ApiController]
    public class CoverLetterController : ControllerBase
    {
        //private readonly GeminiCoverLetterService _coverLetterService;
        private readonly ILogger<CoverLetterController> _logger;
        private readonly CoverLetterService _service;
        private readonly GroqCoverLetterService _grogcoverLetterService;
        private readonly CvStorageService _cvService;
        private readonly TokenService _tokenService;
        private readonly UserService _userService;

        public CoverLetterController(CoverLetterService service,
            GroqCoverLetterService grogcoverLetterService,
            // GeminiCoverLetterService coverLetterService,
            ILogger<CoverLetterController> logger,
            CvStorageService cvService,
            TokenService tokenService,
            UserService userService)
        {
            _grogcoverLetterService = grogcoverLetterService;
            //_coverLetterService = coverLetterService;
            _logger = logger;
            _service = service;
            _cvService = cvService;
            _tokenService = tokenService;
            _userService = userService;
        }

        /// <summary>
        /// Генерирует сопроводительное письмо через Google Gemini AI
        /// </summary>
        /// <param name="request">Данные для генерации письма</param>
        /// <returns>Сгенерированное сопроводительное письмо</returns>
        [HttpPost("generate-gemini")]
        [ProducesResponseType(typeof(CoverLetterResponse), 200)]
        [ProducesResponseType(typeof(CoverLetterResponse), 400)]
        [ProducesResponseType(typeof(CoverLetterResponse), 500)]
        public async Task<ActionResult<CoverLetterResponse>> GenerateCoverLetter(
            [FromBody] CoverLetterRequest request)
        {
            try
            {

                // Валидация
                var validationError = ValidateRequest(request);
                if (validationError != null)
                {
                    return BadRequest(new CoverLetterResponse
                    {
                        Success = false,
                        Error = validationError
                    });
                }

                _logger.LogInformation(
                    "Запрос на генерацию письма: {Name} -> {Company} ({JobTitle})",
                    request.ApplicantName,
                    request.CompanyName,
                    request.JobTitle);

                var (coverLetter, tokensUsed) = await _grogcoverLetterService
                    .GenerateCoverLetterAsync(request);

                return Ok(new CoverLetterResponse
                {
                    Success = true,
                    CoverLetter = coverLetter,
                    GeneratedAt = DateTime.UtcNow,
                    TokensUsed = tokensUsed
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при генерации письма");

                var errorMessage = ex.Message.Contains("API_KEY")
                    ? "Неверный API ключ. Проверьте конфигурацию."
                    : ex.Message.Contains("quota")
                    ? "Превышен лимит запросов. Попробуйте позже."
                    : "Произошла ошибка при генерации письма.";

                return StatusCode(500, new CoverLetterResponse
                {
                    Success = false,
                    Error = errorMessage
                });
            }
        }

        /// <summary>
        /// Проверка работоспособности Gemini API
        /// </summary>
        [HttpGet("health")]
        public async Task<IActionResult> CheckHealth()
        {
            var isHealthy = await _grogcoverLetterService.CheckApiHealthAsync();

            if (isHealthy)
            {
                return Ok(new
                {
                    status = "healthy",
                    message = "Gemini API работает нормально",
                    timestamp = DateTime.UtcNow
                });
            }

            return StatusCode(503, new
            {
                status = "unhealthy",
                message = "Gemini API недоступен. Проверьте API ключ.",
                timestamp = DateTime.UtcNow
            });
        }

        /// <summary>
        /// Получить список доступных моделей
        /// </summary>
        //[HttpGet("models")]
        //public async Task<IActionResult> GetModels()
        //{
        //    try
        //    {
        //        var models = await _coverLetterService.GetAvailableModelsAsync();
        //        return Ok(new
        //        {
        //            success = true,
        //            models = models,
        //            count = models.Count
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Ошибка получения списка моделей");
        //        return StatusCode(500, new
        //        {
        //            success = false,
        //            error = "Не удалось получить список моделей"
        //        });
        //    }
        //}

        private string? ValidateRequest(CoverLetterRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.JobTitle))
                return "Должность обязательна";

            if (string.IsNullOrWhiteSpace(request.CompanyName))
                return "Название компании обязательно";

            if (string.IsNullOrWhiteSpace(request.ApplicantName))
                return "Имя кандидата обязательно";

            if (string.IsNullOrWhiteSpace(request.ApplicantExperience))
                return "Опыт работы обязателен";

            if (string.IsNullOrWhiteSpace(request.ApplicantSkills))
                return "Навыки обязательны";

            return null;
        }

        private string? ValidateRequestForOnlyJobDes(CoverLetterWithOnlyJobDes request)
        {
            if (string.IsNullOrWhiteSpace(request.jobDescription))
                return "обязательна";
            return null;
        }


        [HttpPost("cover-letter-open-ai")]
        public async Task<IActionResult> GenerateCoverLetter(
        [FromBody] CoverLetterRequestDto dto)
        {
            var result = await _service.GenerateChAsync(dto);
            return Ok(new { text = result });
        }

        [HttpPost("text-from-image")]
        public async Task<IActionResult> UploadImage(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("File is empty");

            try
            {
                using var memoryStream = new MemoryStream();
                await file.CopyToAsync(memoryStream);
                var imageBytes = memoryStream.ToArray();

                var tessPath = System.IO.Path.Combine(AppContext.BaseDirectory, "tessdata");

                using var engine = new TesseractEngine(tessPath, "eng");

                using var img = Pix.LoadFromMemory(imageBytes);
                using var page = engine.Process(img);

                var text = page.GetText();

                return Ok(new
                {
                    text = text
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        /// <summary>
        /// Генерирует сопроводительное письмо через Google Gemini AI
        /// </summary>
        /// <param name="request">Данные для генерации письма</param>
        /// <returns>Сгенерированное сопроводительное письмо</returns>
        [HttpPost("cover-letter-with-job-description")]
        [ProducesResponseType(typeof(CoverLetterResponseDes), 200)]
        [ProducesResponseType(typeof(CoverLetterResponseDes), 400)]
        [ProducesResponseType(typeof(CoverLetterResponseDes), 500)]
        public async Task<ActionResult<CoverLetterResponseDes>> generateWithOnlyJobDescription
            ([FromBody] CoverLetterWithOnlyJobDes request)
        {
            try
            {
                // Валидация
                var validationError = ValidateRequestForOnlyJobDes(request);
                if (validationError != null)
                {
                    return BadRequest(new CoverLetterResponseDes
                    {
                        Success = false,
                        Error = validationError
                    });
                }

                var (coverLetter, tokensUsed) = await _grogcoverLetterService
                    .GenerateCoverLetterAsyncWithDes(request);

                return Ok(new CoverLetterResponseDes
                {
                    Success = true,
                    CoverLetter = coverLetter,
                    GeneratedAt = DateTime.UtcNow,
                    TokensUsed = tokensUsed
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при генерации письма");

                var errorMessage = ex.Message.Contains("API_KEY")
                    ? "Неверный API ключ. Проверьте конфигурацию."
                    : ex.Message.Contains("quota")
                    ? "Превышен лимит запросов. Попробуйте позже."
                    : "Произошла ошибка при генерации письма.";

                return StatusCode(500, new CoverLetterResponse
                {
                    Success = false,
                    Error = errorMessage
                });
            }
        }


        [HttpPost("cover-letter-with-job-desc-cv")]
        [ProducesResponseType(typeof(CoverLetterWithOnlyJobDes), 200)]
        [ProducesResponseType(typeof(CoverLetterWithOnlyJobDes), 400)]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<ActionResult<CoverLetterWithOnlyJobDes>> coverletterwithCV
            ([FromBody] CoverLetterWithOnlyJobDes request)
        {
            try
            {
                var userIdClaim = User.FindFirst("userId")?.Value;
                if (userIdClaim == null)
                    return Unauthorized();
                int userId = int.Parse(userIdClaim);

                if (string.IsNullOrEmpty(_cvService.CvText))
                {
                    return BadRequest("You must upload your CV first!");
                }
                // check your tokens
                var yourToken = await _tokenService.GetYourOwnToken(userId);
                if (yourToken == null)
                {
                    return BadRequest(new
                    {
                        message = "You haven't any tokens"
                    });
                }
                if (yourToken.Token <= 0)
                {
                    return BadRequest(new
                    {
                        message = "You haven't any tokens"
                    });
                };
                // request.myCv = _cvService.CvText;
                var (coverLetter, tokensUsed) = await _grogcoverLetterService
                    .GenerateCoverLetterAsyncWithDesAndCv(request);


                await _tokenService.deleteOneToken(userId);
                return Ok(new CoverLetterResponseDes
                {
                    Success = true,
                    CoverLetter = coverLetter,
                    GeneratedAt = DateTime.UtcNow,
                    TokensUsed = tokensUsed
                });
            }
            catch (Exception err)
            {
                _logger.LogError(err, "Ошибка при генерации письма");

                var errorMessage = err.Message.Contains("API_KEY")
                  ? "Неверный API ключ. Проверьте конфигурацию."
                  : err.Message.Contains("quota")
                  ? "Превышен лимит запросов. Попробуйте позже."
                  : "Произошла ошибка при генерации письма.";

                return StatusCode(500, new CoverLetterResponse
                {
                    Success = false,
                    Error = errorMessage
                });
            }
        }

    }
}
