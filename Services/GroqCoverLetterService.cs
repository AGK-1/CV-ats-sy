using System.Text;
using System.Text.Json;
using cvAts.Class;

namespace cvAts.Services
{
    public class GroqCoverLetterService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<GroqCoverLetterService> _logger;
        private readonly string _apiKey;
        private readonly CvStorageService _service;
        private readonly TokenService _tokenService;
        public GroqCoverLetterService(
            HttpClient httpClient,
            ILogger<GroqCoverLetterService> logger,
            IConfiguration configuration,
            CvStorageService service,
            TokenService tokenService)
        {
            _httpClient = httpClient;
            _logger = logger;
            _apiKey = configuration["Groq:ApiKey"]
                ?? throw new Exception("Groq API ключ не найден");
            _service = service;
            _tokenService = tokenService;
        }

        public async Task<(string coverLetter, int tokensUsed)> GenerateCoverLetterAsync(
            CoverLetterRequest request)
        {
            try
            {
                var prompt = BuildPrompt(request);

                _logger.LogInformation("Отправка запроса в Groq API");

                var requestBody = new
                {
                    model = "llama-3.3-70b-versatile", // Быстрая и мощная модель
                    messages = new[]
                    {
                        new
                        {
                            role = "user",
                            content = prompt
                        }
                    },
                    temperature = 0.7,
                    max_tokens = 1500
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");

                var response = await _httpClient.PostAsync(
                    "https://api.groq.com/openai/v1/chat/completions",
                    content
                );

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Ошибка Groq API: {Error}", errorContent);
                    throw new Exception($"Groq API Error: {errorContent}");
                }

                var responseJson = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<JsonElement>(responseJson);

                var coverLetter = result
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString();

                var tokensUsed = result
                    .GetProperty("usage")
                    .GetProperty("total_tokens")
                    .GetInt32();

                _logger.LogInformation("Письмо сгенерировано. Токены: {Tokens}", tokensUsed);

                return (coverLetter ?? "Ошибка", tokensUsed);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка генерации через Groq");
                throw;
            }
        }

        private string BuildPrompt(CoverLetterRequest request)
        {
            return $@"Create a professional cover letter in English.

Vacancy:
Position: {request.JobTitle}
Company: {request.CompanyName}
Description: {request.JobDescription}

Candidate:
Name: {request.ApplicantName}
Experience: {request.ApplicantExperience}
Skills: {request.ApplicantSkills}

REQUIREMENTS:
- 250–350 words
- Professional tone
- Structure: greeting, experience, skills, conclusion
- Highlight skill alignment
- Show enthusiasm

Write ONLY the cover letter text.";
        }

        public async Task<bool> CheckApiHealthAsync()
        {
            try
            {
                var testBody = new
                {
                    model = "llama-3.3-70b-versatile",
                    messages = new[] { new { role = "user", content = "test" } },
                    max_tokens = 5
                };

                var json = JsonSerializer.Serialize(testBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");

                var response = await _httpClient.PostAsync(
                    "https://api.groq.com/openai/v1/chat/completions",
                    content
                );

                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }

        }




        // Get response from AI only for job description
        public async Task<(string coverLetter, int tokensUsed)> GenerateCoverLetterAsyncWithDes(
            CoverLetterWithOnlyJobDes request)
        {
            try
            {
                var prompt = BuildPromptForDescription(request);

                _logger.LogInformation("Отправка запроса в Groq API");

                var requestBody = new
                {
                    model = "llama-3.3-70b-versatile", // Быстрая и мощная модель
                    messages = new[]
                    {
                        new
                        {
                            role = "user",
                            content = prompt
                        }
                    },
                    temperature = 0.7,
                    max_tokens = 1500
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");

                var response = await _httpClient.PostAsync(
                    "https://api.groq.com/openai/v1/chat/completions",
                    content
                );

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Ошибка Groq API: {Error}", errorContent);
                    throw new Exception($"Groq API Error: {errorContent}");
                }

                var responseJson = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<JsonElement>(responseJson);

                var coverLetter = result
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString();

                var tokensUsed = result
                    .GetProperty("usage")
                    .GetProperty("total_tokens")
                    .GetInt32();

                _logger.LogInformation("Письмо сгенерировано. Токены: {Tokens}", tokensUsed);

                return (coverLetter ?? "Ошибка", tokensUsed);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка генерации через Groq");
                throw;
            }
        }
        private string BuildPromptForDescription(CoverLetterWithOnlyJobDes request)
        {
            return $@"Create a professional cover letter in English.
Description: {request.jobDescription}
REQUIREMENTS:
- 250–350 words
- Professional tone
- Structure: greeting, experience, skills, conclusion
- Highlight skill alignment
- Show enthusiasm

Write ONLY the cover letter text.";
        }




        // Get response
        public async Task<(string coverLetter, int tokensUsed)> GenerateCoverLetterAsyncWithDesAndCv(
            CoverLetterWithOnlyJobDes request)
        {
            try
            {
                var prompt = BuildPromptForDescriptionWithCv(request);

                _logger.LogInformation("Отправка запроса в Groq API");

                var requestBody = new
                {
                    model = "llama-3.3-70b-versatile", // Быстрая и мощная модель
                    messages = new[]
                    {
                        new
                        {
                            role = "user",
                            content = prompt
                        }
                    },
                    temperature = 0.7,
                    max_tokens = 1500
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");

                var response = await _httpClient.PostAsync(
                    "https://api.groq.com/openai/v1/chat/completions",
                    content
                );

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Ошибка Groq API: {Error}", errorContent);
                    throw new Exception($"Groq API Error: {errorContent}");
                }

                var responseJson = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<JsonElement>(responseJson);

                var coverLetter = result
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString();

                var tokensUsed = result
                    .GetProperty("usage")
                    .GetProperty("total_tokens")
                    .GetInt32();

                _logger.LogInformation("Письмо сгенерировано. Токены: {Tokens}", tokensUsed);

                return (coverLetter ?? "Ошибка", tokensUsed);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка генерации через Groq");
                throw;
            }
        }
        private string BuildPromptForDescriptionWithCv(CoverLetterWithOnlyJobDes request)
        {
            //            return $@"Create a professional cover letter in English.
            //This is my Cv: {_service.CvText} . Please analyze the CV and extract key skills, experience, and strengths. And get from Cv name and surname and  start cv with that name and surname
            //Description: {request.jobDescription}
            //REQUIREMENTS:
            //- 250–350 words
            //- Professional tone
            //- Structure: greeting, experience, skills, conclusion
            //- Highlight skill alignment
            //- Show enthusiasm

            //Write ONLY the cover letter text.";
            return $@"
Create a professional cover letter in English.

CV:
{_service.CvText}

Job Description:
{request.jobDescription}

Instructions:
- Analyze the CV and identify the candidate's skills and experience.
- Extract the candidate's full name from the CV.
- Start the letter with 'Dear Hiring Manager,'.
- Use the candidate's name in the signature at the end.
- 250-350 words.
- Professional tone.
- 4-5 paragraphs.
- Explain how the candidate's skills match the job requirements.
- Show enthusiasm for the role.

Output rules:
- Plain text only.
- No markdown.
- No bullet points.
- No headings.
- No URLs or hyperlinks.
- No special formatting.
- Return only the cover letter text.

";
        }
    }
}



