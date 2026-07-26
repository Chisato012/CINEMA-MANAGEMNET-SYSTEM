using Cinema_Management.Models.Chatbot;
using Cinema_Management.Models.Recommendation;

namespace Cinema_Management.Services.Chatbot;

public interface IChatbotService
{
    Task<ChatbotResponse> RecommendAsync(
        GenreRecommendationInput input,
        CancellationToken cancellationToken);
}
