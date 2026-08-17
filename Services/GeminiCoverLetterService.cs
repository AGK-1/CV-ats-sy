using System.Text;
using System.Text.Json;
using cvAts.Class;

namespace cvAts.Services
{
    public class GeminiCoverLetterService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<GeminiCoverLetterService> _logger;
        private readonly string _apiKey;
        private readonly string _model;

        public GeminiCoverLetterService(
            HttpClient httpClient,
            ILogger<GeminiCoverLetterService> logger,
            IConfiguration configuration)
        {
            _httpClient = httpClient;
            _logger = logger;
            _apiKey = configuration["Gemini:ApiKey"]
                ?? throw new Exception("Gemini API ключ не найден в конфигурации");
            _model = configuration["Gemini:Model"] ?? "gemini-2.0-flash-exp";
        }

        public async Task<(string coverLetter, int tokensUsed)> GenerateCoverLetterAsync(
            CoverLetterRequest request)
        {
            try
            {
                var prompt = BuildPrompt(request);

                _logger.LogInformation(
                    "Отправка запроса в Gemini для {JobTitle} в {Company}",
                    request.JobTitle,
                    request.CompanyName);

                var requestBody = new
                {
                    contents = new[]
                    {
                        new
                        {
                            parts = new[]
                            {
                                new { text = prompt }
                            }
                        }
                    },
                    generationConfig = new
                    {
                        temperature = 0.7,
                        topK = 40,
                        topP = 0.95,
                        maxOutputTokens = 1500,
                        candidateCount = 1
                    },
                    safetySettings = new[]
                    {
                        new
                        {
                            category = "HARM_CATEGORY_HARASSMENT",
                            threshold = "BLOCK_MEDIUM_AND_ABOVE"
                        },
                        new
                        {
                            category = "HARM_CATEGORY_HATE_SPEECH",
                            threshold = "BLOCK_MEDIUM_AND_ABOVE"
                        }
                    }
                };

                var json = JsonSerializer.Serialize(requestBody, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:generateContent?key={_apiKey}";

                var response = await _httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Ошибка Gemini API: {StatusCode}, {Error}",
                        response.StatusCode, errorContent);
                    throw new Exception($"Gemini API Error: {response.StatusCode}. {errorContent}");
                }

                var responseJson = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<JsonElement>(responseJson);

                // Извлекаем текст
                var coverLetter = result
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString();

                // Извлекаем информацию о токенах (опционально)
                var tokensUsed = 0;
                if (result.TryGetProperty("usageMetadata", out var metadata))
                {
                    tokensUsed = metadata.GetProperty("totalTokenCount").GetInt32();
                }

                _logger.LogInformation(
                    "Письмо успешно сгенерировано. Использовано токенов: {Tokens}",
                    tokensUsed);

                return (coverLetter ?? "Ошибка генерации", tokensUsed);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при генерации письма через Gemini");
                throw;
            }
        }

        private string BuildPrompt(CoverLetterRequest request)
        {
            var languageInstruction = request.Language.ToLower() == "english"
                ? "Write in English."
                : "Напиши на русском языке.";

            return $@"Ты профессиональный карьерный консультант. Создай качественное сопроводительное письмо.

{languageInstruction}

ИНФОРМАЦИЯ О ВАКАНСИИ:
Должность: {request.JobTitle}
Компания: {request.CompanyName}
Описание вакансии: {request.JobDescription}

ИНФОРМАЦИЯ О КАНДИДАТЕ:
Имя: {request.ApplicantName}
Опыт работы: {request.ApplicantExperience}
Ключевые навыки: {request.ApplicantSkills}

ТРЕБОВАНИЯ К ПИСЬМУ:
1. Длина: 250-350 слов
2. Структура:
   - Приветствие и вступление (1 абзац)
   - Основная часть: опыт и навыки (2-3 абзаца)
   - Заключение с призывом к действию
3. Тон: профессиональный, но дружелюбный
4. Подчеркни соответствие навыков кандидата требованиям вакансии
5. Покажи энтузиазм и мотивацию работать именно в этой компании
6. Избегай клише и шаблонных фраз
7. Начни с обращения к компании

Напиши ТОЛЬКО текст сопроводительного письма, без дополнительных комментариев или объяснений.";
        }

        // Проверка работоспособности API
        public async Task<bool> CheckApiHealthAsync()
        {
            try
            {
                var testUrl = $"https://generativelanguage.googleapis.com/v1beta/models?key={_apiKey}";
                var response = await _httpClient.GetAsync(testUrl);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        // Получить список доступных моделей
        public async Task<List<string>> GetAvailableModelsAsync()
        {
            try
            {
                var url = $"https://generativelanguage.googleapis.com/v1beta/models?key={_apiKey}";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                    return new List<string>();

                var json = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<JsonElement>(json);

                var models = new List<string>();
                if (result.TryGetProperty("models", out var modelsArray))
                {
                    foreach (var model in modelsArray.EnumerateArray())
                    {
                        if (model.TryGetProperty("name", out var name))
                        {
                            models.Add(name.GetString() ?? "");
                        }
                    }
                }

                return models;
            }
            catch
            {
                return new List<string>();
            }
        }
    }
}
