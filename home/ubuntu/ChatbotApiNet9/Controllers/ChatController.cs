using ChatbotApiNet9.Interfaces;
using ChatbotApiNet9.Models;
using Microsoft.AspNetCore.Mvc;

namespace ChatbotApiNet9.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly IChatService _chatService;
    private readonly ILogger<ChatController> _logger;

    public ChatController(IChatService chatService, ILogger<ChatController> logger)
    {
        _chatService = chatService;
        _logger = logger;
    }

    [HttpPost("message")]
    [ProducesResponseType(typeof(ChatMessageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> PostMessage([FromBody] ChatMessageRequest request)
    {
        if (!ModelState.IsValid)
        { 
            _logger.LogWarning("Invalid model state for ChatMessageRequest.");
            return BadRequest(ModelState);
        }

        try
        {
            _logger.LogInformation("Processing message from user {UserId} in session {SessionId}", request.UserId, request.SessionId ?? "N/A");
            var response = await _chatService.ProcessMessageAsync(request);
            _logger.LogInformation("Successfully processed message for user {UserId}", request.UserId);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message for user {UserId}", request.UserId);
            // Consider returning a more user-friendly error message depending on the context
            return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred while processing your request.");
        }
    }
}

