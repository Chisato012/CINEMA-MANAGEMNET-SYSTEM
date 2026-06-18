using Customer.Models;
using Customer.Services;
using Microsoft.AspNetCore.Mvc;

namespace Customer.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class ChatController(ChatOrchestrator chatOrchestrator) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ChatResponse>> Chat(
        [FromBody] ChatRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest("Message is required.");
        }

        var response = await chatOrchestrator.AnswerAsync(request, cancellationToken);
        return Ok(response);
    }
}
