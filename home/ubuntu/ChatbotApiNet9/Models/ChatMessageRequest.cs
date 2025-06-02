namespace ChatbotApiNet9.Models;

public class ChatMessageRequest
{
    public required string UserId { get; set; }
    public required string Message { get; set; }
    public string? SessionId { get; set; }
}