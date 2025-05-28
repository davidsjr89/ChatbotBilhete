namespace ChatbotApiNet9.Interfaces;
public interface IAiService
{
    // Retorna uma resposta baseada na mensagem do usuário e no contexto da sessão (opcional)
    Task<string> GetResponseAsync(string userMessage, string? sessionContext = null);
    
    // Método para gerar resposta com base apenas na mensagem do usuário (usado pelo ChatService)
    Task<string> GenerateResponseAsync(string userMessage);
}
