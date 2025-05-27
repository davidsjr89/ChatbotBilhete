using ChatbotApiNet9.Models;

namespace ChatbotApiNet9.Interfaces;

public interface IChatService
{
    Task<ChatMessageResponse> ProcessMessageAsync(ChatMessageRequest request);
}

