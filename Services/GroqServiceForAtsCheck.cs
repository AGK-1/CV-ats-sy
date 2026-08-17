using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

public class GroqServiceForAtsCheck
{
    private readonly HttpClient _http;

    public GroqServiceForAtsCheck(HttpClient http, IConfiguration config)
    {
        _http = http;
        _http.BaseAddress = new Uri("https://api.groq.com/openai/v1/");

        var apiKey = config["Groq:ApiKey"]
            ?? throw new Exception("Groq API key missing");

        _http.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", apiKey);
    }

    public async Task<string> AnalyzeCvAsync(string cvText, string jobDescription)
    {
       // string jobDescription = "Backend Java";
        // ❗ ОБЯЗАТЕЛЬНО ограничиваем длину
        if (cvText.Length > 12000)
            cvText = cvText.Substring(0, 12000);

        var prompt = $"""
You are an ATS (Applicant Tracking System) analyzer.

Analyze the following CV for the given job description. 
Return JSON with:
- only score with precent (0-100)
and quick feedback with a 200 symbol explaining why your score for this vacancy is what it is.
Job Description:
<<<
{jobDescription}
>>>

CV Text:
<<<
{cvText}
>>>
""";




        var model = "llama-3.3-70b-versatile"; // <- актуальная модель из списка
        var body = new
        {
            model = model,
            messages = new[]
            {
        new
        {
            role = "user",
            content = prompt
        }
    }
        };

        var json = JsonSerializer.Serialize(body);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _http.PostAsync("chat/completions", content);

        // 🔥 ЕСЛИ ОШИБКА — ЧИТАЕМ ТЕЛО
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"Groq error {response.StatusCode}: {error}");
        }

        return await response.Content.ReadAsStringAsync();
    }
}
