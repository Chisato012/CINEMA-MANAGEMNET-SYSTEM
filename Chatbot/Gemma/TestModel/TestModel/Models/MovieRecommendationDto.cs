namespace TestModel.Models
{
    public class MovieRecommendationDto
    {
        public int MovieId { get; set; }
        public string Title { get; set; } = "";
        public string? Synopsis { get; set; }
        public string? PosterUrl { get; set; }
        public string? Trailer { get; set; }
        public string? AgeRating { get; set; }
        public int Duration { get; set; }
        public string? Genres { get; set; }
        public DateTime? NextShowtime { get; set; }
        public decimal? BasePrice { get; set; }
    }
}
