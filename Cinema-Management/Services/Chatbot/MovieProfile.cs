namespace Cinema_Management.Services.Chatbot;

public class MovieProfile
{
    public int MovieId { get; set; }
    public string Title { get; set; } = string.Empty;
    public short Duration { get; set; }
    public string AgeRating { get; set; } = string.Empty;
    public string Synopsis { get; set; } = string.Empty;
    public List<string> Genres { get; set; } = [];
    public List<DateTime> Showtimes { get; set; } = [];
}
