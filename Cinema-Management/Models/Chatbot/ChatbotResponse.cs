namespace Cinema_Management.Models.Chatbot;

public class ChatbotResponse
{
    public string Intent { get; set; } = "fallback";
    public string? Mood { get; set; }
    public string? RecommendedGenreCode { get; set; }
    public string? RecommendedGenreName { get; set; }
    public string Reply { get; set; } = string.Empty;
    public IReadOnlyList<ChatbotMovieResult> Movies { get; set; } = [];
}
