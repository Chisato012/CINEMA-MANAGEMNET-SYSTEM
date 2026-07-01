using System.Text.Json.Serialization;

namespace TestModel.Models
{
    public class LmStudioChatRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("messages")]
        public List<LmStudioMessage> Messages { get; set; } = new();

        [JsonPropertyName("temperature")]
        public double Temperature { get; set; } = 0.7;

        [JsonPropertyName("top_p")]
        public double TopP { get; set; } = 0.9;

        [JsonPropertyName("max_tokens")]
        public int MaxTokens { get; set; } = 200;
    }

    public class LmStudioMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = string.Empty;

        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;
    }

    public class LmStudioChatResponse
    {
        [JsonPropertyName("choices")]
        public List<LmStudioChoice> Choices { get; set; } = new();
    }

    public class LmStudioChoice
    {
        [JsonPropertyName("message")]
        public LmStudioMessage Message { get; set; } = new();
    }
}
