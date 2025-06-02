using ChatbotApiNet9.Interfaces;
using ChatbotApiNet9.Models;
using Microsoft.AspNetCore.Authorization; // Required for [Authorize]
using Microsoft.AspNetCore.Mvc;

namespace ChatbotApiNet9.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
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
    [ProducesResponseType(StatusCodes.Status401Unauthorized)] // Added for authorization failure
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> PostMessage([FromBody] ChatMessageRequest request)
    {
        var username = User.Identity?.Name;

        if (string.IsNullOrEmpty(username))
        {
            _logger.LogWarning("Unauthorized attempt to post message: Username claim missing from token.");
            return Unauthorized(new { Message = "Invalid token: User identifier missing." });
        }

        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Invalid model state for ChatMessageRequest for user {Username}.", username);
            return BadRequest(ModelState);
        }

        try
        {
            _logger.LogInformation("Processing message from authenticated user {Username} in session {SessionId}", username, request.SessionId ?? "N/A");
            var updatedRequest = new ChatMessageRequest
            {
                UserId = username,
                Message = request.Message,
                SessionId = request.SessionId // Ou você pode modificar o SessionId conforme necessário
            };

            var response = await _chatService.ProcessMessageAsync(updatedRequest);
            _logger.LogInformation("Successfully processed message for user {Username}", username);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message for user {Username}", username);
            return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse { Message = "An unexpected error occurred while processing your request." });
        }
    }
}

