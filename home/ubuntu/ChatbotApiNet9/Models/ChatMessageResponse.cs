namespace ChatbotApiNet9.Models;

public class ChatMessageResponse
{
    public string? Response { get; set; }
    public string? SessionId { get; set; }
    public object? ActionData { get; set; } // Para retornar dados específicos (ex: detalhes de voo)
}

