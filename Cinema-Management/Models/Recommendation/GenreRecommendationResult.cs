namespace Cinema_Management.Models.Recommendation;

public class GenreRecommendationResult
{
    public string GenreCode { get; set; } = string.Empty;
    public string GenreName { get; set; } = string.Empty;
    public float Confidence { get; set; }
}
