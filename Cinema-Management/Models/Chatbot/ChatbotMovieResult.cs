namespace Cinema_Management.Models.Chatbot;

public class ChatbotMovieResult
{
    public int MovieId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Genres { get; set; } = string.Empty;
    public string AgeRating { get; set; } = string.Empty;
    public short Duration { get; set; }
    public string Synopsis { get; set; } = string.Empty;
    public string? NextShowtime { get; set; }
    public int ShowtimeCount { get; set; }
    public double Score { get; set; }
}
