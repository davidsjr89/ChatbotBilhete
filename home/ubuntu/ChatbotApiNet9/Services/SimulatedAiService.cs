using ChatbotApiNet9.Interfaces;
namespace ChatbotApiNet9.Services;
// Simulação de um serviço de IA gratuito (respostas simples e pré-definidas)
public class SimulatedAiService : IAiService
{
    private readonly ILogger<SimulatedAiService> _logger;
    private static readonly Random _random = new Random();
    private static readonly List<string> _genericResponses = new List<string>
    {
        "Interessante. Conte-me mais!",
        "Entendo.",
        "Hmm, isso é algo para se pensar.",
        "Não tenho certeza sobre isso, mas posso tentar ajudar com passagens aéreas.",
        "Que tal falarmos sobre viagens? Posso buscar voos para você.",
        "Isso foge um pouco da minha especialidade, que é passagens aéreas.",
        "Legal!"
    };
    public SimulatedAiService(ILogger<SimulatedAiService> logger)
    {
        _logger = logger;
    }

    public Task<string> GetResponseAsync(string userMessage, string? sessionContext = null)
    {
        _logger.LogInformation("Simulating AI response for message: '{UserMessage}' with context: {SessionContext}", userMessage, sessionContext ?? "None");
        // Lógica de IA muito simples: retorna uma resposta genérica aleatória
        // Uma IA real analisaria a mensagem e o contexto para dar uma resposta mais relevante.
        int index = _random.Next(_genericResponses.Count);
        string response = _genericResponses[index];
        _logger.LogInformation("Generated simulated AI response: '{Response}'", response);
        // Simula um pequeno atraso, como se estivesse processando
        // await Task.Delay(100); // Descomentar para simular latência
        return Task.FromResult(response);
    }

    // Implementação do método GenerateResponseAsync usado pelo ChatService
    public Task<string> GenerateResponseAsync(string userMessage)
    {
        _logger.LogInformation("Generating AI response for message: '{UserMessage}'", userMessage);
        // Reutiliza a mesma lógica do GetResponseAsync, mas sem contexto
        return GetResponseAsync(userMessage);
    }
}
