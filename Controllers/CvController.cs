using System.Security.Claims;
using System.Text;
using System.Text.Json;
using cvAts.Class;
using cvAts.Services;
using DocumentFormat.OpenXml.Packaging;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace cvAts.Controllers
{
    [Route("api/[controller]")]
    [ApiController]

    public class CvController : ControllerBase
    {
        private readonly CvStorageService _cvService;
        private readonly AppDbContext _context;
        public CvController(CvStorageService cvService, AppDbContext context)
        {
            _context = context;
            _cvService = cvService;
        }

        [HttpPost("upload-pdf-cv")]
        [Consumes("multipart/form-data")]
        public IActionResult UploadPdf(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("PDF file not found");

            var text = new StringBuilder();

            using var stream = file.OpenReadStream();
            using var reader = new PdfReader(stream);
            using var pdf = new PdfDocument(reader);

            for (int i = 1; i <= pdf.GetNumberOfPages(); i++)
            {
                text.Append(
                    PdfTextExtractor.GetTextFromPage(pdf.GetPage(i))
                );
            }
            _cvService.CvText = text.ToString();
            return Ok(new
            {
                fileName = file.FileName,
                content = text.ToString()
            });
        }



        [HttpPost("upload-word-cv")]
        [Consumes("multipart/form-data")]
        public IActionResult UploadWord(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("File not found");

            using var stream = file.OpenReadStream();

            using var doc = WordprocessingDocument.Open(stream, false);
            var body = doc.MainDocumentPart.Document.Body;

            var text = body.InnerText;
            _cvService.CvText = text.ToString();
            return Ok(new
            {
                fileName = file.FileName,
                content = text
            });
        }

        [HttpPost("upload-your-cv")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Файл не выбран");

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            var allowedPdfExtensions = new[] { ".pdf" };
            var allowedWordExtensions = new[] { ".doc", ".docx" };

            string content = null;

            if (allowedPdfExtensions.Contains(extension))
            {
                content = await UploadPdfc(file);  // приватный метод возвращает string
            }
            else if (allowedWordExtensions.Contains(extension))
            {
                content = await UploadWordc(file); // приватный метод возвращает string
            }
            else
            {
                return BadRequest("Неподдерживаемый формат файла");
            }

            return Ok(new
            {
                fileName = file.FileName,
                content
            });
        }


        private async Task<string> UploadPdfc(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return null;

            var text = new StringBuilder();

            using var stream = file.OpenReadStream();
            using var reader = new PdfReader(stream);
            using var pdf = new PdfDocument(reader);

            for (int i = 1; i <= pdf.GetNumberOfPages(); i++)
            {
                text.Append(
                    PdfTextExtractor.GetTextFromPage(pdf.GetPage(i))
                );
            }
            _cvService.CvText = text.ToString();
            return text.ToString();

        }

        // ========= WORD =========
        private async Task<string> UploadWordc(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return null;

            using var stream = file.OpenReadStream();

            using var doc = WordprocessingDocument.Open(stream, false);
            var body = doc.MainDocumentPart.Document.Body;

            var text = body.InnerText;
            _cvService.CvText = text;
            return text.ToString();
        }

        
        [HttpPost("save-your-cv")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> SaveYourCV([FromBody] JsonDocument data)
        {
            var userIdClaim = User.FindFirst("userId");

            if (userIdClaim == null)
                return Unauthorized("userId claim not found");

            int userId = int.Parse(userIdClaim.Value);

            var cv = new Mycv
            {
                UserId = userId,
                Data = data,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Mycvs.Add(cv);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                cv.Id
            });
        }


        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpGet("me")]
        public IActionResult Me()
        {
            return Ok(User.Claims.Select(c => new
            {
                c.Type,
                c.Value
            }));
        }
    }



}
