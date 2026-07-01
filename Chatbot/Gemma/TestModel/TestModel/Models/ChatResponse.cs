namespace TestModel.Models
{
    public class ChatResponse
    {
        public string Answer { get; set; } = "";
        public string? Mood { get; set; }
        public string? Keywords { get; set; }
        public List<MovieRecommendationDto> Movies { get; set; } = new();
    }
}