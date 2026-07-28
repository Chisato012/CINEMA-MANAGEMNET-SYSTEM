namespace Cinema_Management.Models.Recommendation;

public class GenreRecommendationInput
{
    public string Mood { get; set; } = string.Empty;
    public string Companion { get; set; } = string.Empty;
    public string Intensity { get; set; } = string.Empty;
    public string AgeRating { get; set; } = string.Empty;
}
