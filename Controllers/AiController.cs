using cvAts.DTO;
using cvAts.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace cvAts.Controllers
{
    [ApiController]
    [Route("api/ats/")]
    
    public class AiController : ControllerBase
    {
        private readonly PdfService _pdfService;
        private readonly GroqServiceForAtsCheck _groqService;

        public AiController(
            PdfService pdfService,
            GroqServiceForAtsCheck groqService)
        {
            _pdfService = pdfService;
            _groqService = groqService;
        }

        [HttpPost("analyze")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Analyze([FromForm] CvUploadDto request)
        {
            var description = request.Description;
            if (request?.Cv == null || request.Cv.Length == 0)
                return BadRequest("PDF is required");

            var tempFile = Path.GetTempFileName();

            using (var stream = System.IO.File.Create(tempFile))
            {
                await request.Cv.CopyToAsync(stream);
            }

            var text = _pdfService.ExtractText(tempFile);
            var result = await _groqService.AnalyzeCvAsync(text, description);

            System.IO.File.Delete(tempFile);

            return Content(result, "application/json");
        }





    }
}
