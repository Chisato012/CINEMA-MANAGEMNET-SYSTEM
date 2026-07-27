using Cinema_Management.Models.Recommendation;

namespace Cinema_Management.Services.Recommendation;

public interface IGenreRecommendationService
{
    GenreRecommendationResult? Predict(GenreRecommendationInput input);
}
