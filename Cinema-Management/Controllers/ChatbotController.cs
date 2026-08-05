using Cinema_Management.Models.Chatbot;
using Cinema_Management.Models.Recommendation;
using Cinema_Management.Services.Chatbot;
using Microsoft.AspNetCore.Mvc;

namespace Cinema_Management.Controllers;

[ApiController]
[Route("api/chatbot")]
public class ChatbotController : ControllerBase
{
    private readonly IChatbotService _chatbotService;

    public ChatbotController(IChatbotService chatbotService)
    {
        _chatbotService = chatbotService;
    }

     
     
     
    [HttpPost("recommend")]
    public async Task<ActionResult<ChatbotResponse>> Recommend(
        [FromBody] GenreRecommendationInput request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Mood)
            || string.IsNullOrWhiteSpace(request.Companion)
            || string.IsNullOrWhiteSpace(request.Intensity)
            || string.IsNullOrWhiteSpace(request.AgeRating))
        {
            return BadRequest(new
            {
                message = "Vui lòng chọn đầy đủ tâm trạng, người đi cùng, nhịp phim và độ tuổi."
            });
        }

        var response = await _chatbotService.RecommendAsync(request, cancellationToken);
        return Ok(response);
    }
}
