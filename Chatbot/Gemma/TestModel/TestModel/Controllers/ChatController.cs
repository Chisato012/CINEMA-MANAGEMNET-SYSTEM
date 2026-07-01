using Microsoft.AspNetCore.Mvc;
using TestModel.Models;
using TestModel.Services;

namespace TestModel.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatController : ControllerBase
    {
        private readonly ILmStudioService _lmStudioService;
        private readonly MongoRecommendationService _mongoRecommendationService;
        private readonly MovieQueryService _movieQueryService;

        public ChatController(
            ILmStudioService lmStudioService,
            MongoRecommendationService mongoRecommendationService,
            MovieQueryService movieQueryService)
        {
            _lmStudioService = lmStudioService;
            _mongoRecommendationService = mongoRecommendationService;
            _movieQueryService = movieQueryService;
        }

        [HttpPost("recommend")]
        public async Task<ActionResult<ChatResponse>> Recommend([FromBody] ChatRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest("Message không được rỗng.");
            }

            var llmResult = await _lmStudioService.AskAsync(request.Message);

            var movieIds = await _mongoRecommendationService.FindMovieIdsAsync(
                llmResult.Mood,
                llmResult.Keywords
            );

            var movies = movieIds.Count > 0
                ? await _movieQueryService.GetMoviesByIdsAsync(movieIds)
                : await _movieQueryService.GetFallbackMoviesAsync();

            llmResult.Movies = movies;

            return Ok(llmResult);
        }
    }
}
