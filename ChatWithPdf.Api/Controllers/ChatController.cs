using ChatWithPdf.Api.Contracts.Requests;
using ChatWithPdf.Application.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace ChatWithPdf.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class ChatController(IRagChatService ragChatService) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(ChatAnswer), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ChatAnswer>> Ask(
        [FromBody] ChatRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Question))
        {
            return BadRequest(new { error = "Question is required." });
        }

        var answer = await ragChatService.AskAsync(
            new ChatQuestion(request.Question, request.DocumentId),
            cancellationToken);

        return Ok(answer);
    }
}
