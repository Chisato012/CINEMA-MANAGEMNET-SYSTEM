using System.Text.RegularExpressions;
using TestModel.Models;

namespace TestModel.Services
{
    public class LmStudioService : ILmStudioService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public LmStudioService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public async Task<ChatResponse> AskAsync(string userMessage)
        {
            var modelName = _configuration["LmStudio:Model"];

            if (string.IsNullOrWhiteSpace(modelName))
            {
                throw new Exception("Chưa cấu hình LmStudio:Model trong appsettings.json");
            }

            var request = new LmStudioChatRequest
            {
                Model = modelName,
                Temperature = 0.7,
                TopP = 0.9,
                MaxTokens = 200,
                Messages =
            {
                new LmStudioMessage
                {
                    Role = "system",
                    Content = """
                    Bạn là chatbot gợi ý phim cho website đặt vé xem phim.
                    Hãy trả lời ngắn gọn, thân thiện.
                    Luôn tạo thẻ <recommend> ở cuối câu trả lời theo dạng:
                    <recommend>mood='...', keywords='...'</recommend>
                    """
                },
                new LmStudioMessage
                {
                    Role = "user",
                    Content = userMessage
                }
            }
            };

            var response = await _httpClient.PostAsJsonAsync("chat/completions", request);

            if (!response.IsSuccessStatusCode)
            {
                var errorText = await response.Content.ReadAsStringAsync();
                throw new Exception($"LM Studio lỗi: {response.StatusCode} - {errorText}");
            }

            var result = await response.Content.ReadFromJsonAsync<LmStudioChatResponse>();

            var answer = result?.Choices.FirstOrDefault()?.Message.Content ?? "";

            var mood = ExtractValue(answer, "mood");
            var keywords = ExtractValue(answer, "keywords");

            return new ChatResponse
            {
                Answer = answer,
                Mood = mood,
                Keywords = keywords
            };
        }

        private static string? ExtractValue(string text, string fieldName)
        {
            var pattern = fieldName + @"='(.*?)'";
            var match = Regex.Match(text, pattern);

            return match.Success ? match.Groups[1].Value : null;
        }
    }
}
