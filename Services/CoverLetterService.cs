using System.Net.Http.Headers;
using System.Text;
using OpenAI.Chat;
using System.Text.Json;
namespace cvAts.Services
{
    public class CoverLetterService
    {
        private readonly ChatClient _chatClient;
        //private readonly string _hfApiKey;

        public CoverLetterService(IConfiguration config)
        {
            var apiKey = config["OpenAI:ApiKey"];

            _chatClient = new ChatClient(model: "gpt-4.1-mini", apiKey);

           
        }

        public async Task<string> GenerateChAsync(CoverLetterRequestDto dto)
        {
            var prompt = $"""
        You are a professional HR assistant.

 

        Candidate name: {dto.FullName}
        Position: {dto.Position}
        Company: {dto.Company}
        Skills: {dto.Skills}
        Experience: {dto.Experience}

        The cover letter should be concise, professional, and tailored to the company.
        """;

            var response = await _chatClient.CompleteChatAsync(
                new ChatMessage[]
                {
                new UserChatMessage(prompt)
                }
            );

            return response.Value.Content[0].Text;
        }
       
    }


        
}
