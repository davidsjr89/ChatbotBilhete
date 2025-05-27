namespace ChatbotApiNet9.Interfaces;

public interface IAiService
{
    // Retorna uma resposta baseada na mensagem do usuário e no contexto da sessão (opcional)
    Task<string> GetResponseAsync(string userMessage, string? sessionContext = null);
}

