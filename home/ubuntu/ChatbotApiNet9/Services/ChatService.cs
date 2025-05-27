using ChatbotApiNet9.Interfaces;
using ChatbotApiNet9.Models;
using System.Text.Json; // Para serializar/desserializar dados de voo
using System.Text.RegularExpressions; // Para extrair informações da mensagem

namespace ChatbotApiNet9.Services;

public class ChatService : IChatService
{
    private readonly ILogger<ChatService> _logger;
    private readonly ITicketService _ticketService;
    private readonly IAiService _aiService;

    // Simulação de estado de sessão (poderia ser um cache distribuído em produção)
    // Agora armazena um objeto para guardar mais contexto
    private static readonly Dictionary<string, SessionState> _sessionContext = new Dictionary<string, SessionState>();

    public ChatService(ILogger<ChatService> logger, ITicketService ticketService, IAiService aiService)
    {
        _logger = logger;
        _ticketService = ticketService;
        _aiService = aiService;
    }

    public async Task<ChatMessageResponse> ProcessMessageAsync(ChatMessageRequest request)
    {
        _logger.LogInformation("Processing message: '{Message}' for user {UserId}", request.Message, request.UserId);

        string sessionId = request.SessionId ?? Guid.NewGuid().ToString();
        SessionState currentState = _sessionContext.GetValueOrDefault(sessionId, new SessionState { CurrentIntent = Intent.None });

        string responseMessage;
        object? actionData = null;

        // Lógica aprimorada com detecção de intenção e contexto de sessão
        Intent detectedIntent = DetectIntent(request.Message, currentState);

        switch (detectedIntent)
        {
            case Intent.SearchFlights:
                var searchParams = ExtractFlightSearchParams(request.Message);
                if (searchParams.IsValid)
                {
                    var flights = await _ticketService.SearchFlightsAsync(searchParams.Origin ?? "", searchParams.Destination!, searchParams.Date!.Value);
                    if (flights.Any())
                    {
                        responseMessage = $"Encontrei {flights.Count} voos para {searchParams.Destination} em {searchParams.Date:dd/MM/yyyy}. Qual você gostaria de reservar? (Informe o número do voo)";
                        actionData = flights; // Retorna a lista de voos encontrados
                        currentState.CurrentIntent = Intent.WaitingForFlightSelection;
                        currentState.ContextData = JsonSerializer.Serialize(flights); // Guarda os voos no contexto
                    }
                    else
                    {
                        responseMessage = $"Desculpe, não encontrei voos de {searchParams.Origin ?? ""} para {searchParams.Destination} na data {searchParams.Date:dd/MM/yyyy}. Gostaria de tentar outra data?";
                        currentState.CurrentIntent = Intent.None;
                        currentState.ContextData = null;
                    }
                }
                else
                {
                    responseMessage = "Para pesquisar voos, por favor, me diga o destino e a data (ex: 'voo para Lisboa em 15/07/2025').";
                    currentState.CurrentIntent = Intent.WaitingForFlightDetails;
                }
                break;

            case Intent.BookFlight:
                var flightNumberToBook = ExtractFlightNumber(request.Message);
                if (!string.IsNullOrEmpty(flightNumberToBook))
                {
                    // Verifica se o voo está no contexto da pesquisa anterior
                    List<Flight>? previousFlights = null;
                    if (currentState.ContextData != null)
                    {
                        try { previousFlights = JsonSerializer.Deserialize<List<Flight>>(currentState.ContextData); }
                        catch (JsonException ex) { _logger.LogWarning(ex, "Failed to deserialize flight context for session {SessionId}", sessionId); }
                    }

                    if (previousFlights != null && previousFlights.Any(f => f.FlightNumber.Equals(flightNumberToBook, StringComparison.OrdinalIgnoreCase)))
                    {
                        bool booked = await _ticketService.BookFlightAsync(flightNumberToBook, request.UserId);
                        if (booked)
                        {
                            responseMessage = $"Ótimo! Seu voo {flightNumberToBook} foi reservado com sucesso.";
                            currentState.CurrentIntent = Intent.None; // Reseta a intenção
                            currentState.ContextData = null;
                        }
                        else
                        {
                            responseMessage = $"Houve um problema ao tentar reservar o voo {flightNumberToBook}. Por favor, tente novamente mais tarde.";
                            // Mantém o contexto para possível retentativa
                        }
                    }
                    else
                    {
                        responseMessage = $"Não encontrei o voo {flightNumberToBook} na sua pesquisa recente. Por favor, verifique o número ou faça uma nova busca.";
                        // Mantém o contexto de seleção
                    }
                }
                else
                {
                    responseMessage = "Por favor, informe o número do voo que deseja reservar (ex: 'reservar voo AZ101').";
                    // Mantém o contexto de seleção
                }
                break;

            case Intent.Help:
                responseMessage = "Olá! Como posso te ajudar hoje? Você pode me pedir para pesquisar passagens aéreas (ex: 'buscar voo para Paris em 10/08/2025') ou apenas conversar.";
                currentState.CurrentIntent = Intent.None;
                currentState.ContextData = null;
                break;

            case Intent.Greeting:
                 responseMessage = "Olá! Em que posso ajudar?";
                 currentState.CurrentIntent = Intent.None;
                 currentState.ContextData = null;
                 break;

            case Intent.None: // Nenhuma intenção específica detectada, usar IA
            default:
                responseMessage = await _aiService.GetResponseAsync(request.Message, currentState.ToString()); // Passa o estado como contexto para a IA
                // A IA poderia potencialmente mudar o estado/intenção, mas aqui simplificamos
                break;
        }

        _sessionContext[sessionId] = currentState; // Atualiza o estado da sessão

        var response = new ChatMessageResponse
        {
            Response = responseMessage,
            SessionId = sessionId,
            ActionData = actionData
        };

        _logger.LogInformation("Generated response:	'{Response}	' for session {SessionId}", response.Response, response.SessionId);

        return response;
    }

    // --- Métodos Auxiliares --- 

    private enum Intent { None, Greeting, Help, SearchFlights, BookFlight, WaitingForFlightDetails, WaitingForFlightSelection }

    private class SessionState
    {
        public Intent CurrentIntent { get; set; } = Intent.None;
        public string? ContextData { get; set; } // Armazena dados relevantes (ex: JSON de voos encontrados)
        public override string ToString() => $"Intent: {CurrentIntent}, Context: {(string.IsNullOrEmpty(ContextData) ? "Empty" : "Present")}";
    }

    private Intent DetectIntent(string message, SessionState currentState)
    {
        // Lógica simples de detecção de palavras-chave e contexto
        message = message.ToLowerInvariant();

        if (Regex.IsMatch(message, @"\b(olá|oi|bom dia|boa tarde|boa noite)\b")) return Intent.Greeting;
        if (Regex.IsMatch(message, @"\b(ajuda|socorro|help|help me)\b")) return Intent.Help;
        if (Regex.IsMatch(message, @"\b(busca\w*|pesquis\w*|procur\w*|achar|acha|encontra\w*)\s+(voo|passagem)\b") || 
            (currentState.CurrentIntent == Intent.WaitingForFlightDetails && ExtractFlightSearchParams(message).IsValid))
            return Intent.SearchFlights;
        if (Regex.IsMatch(message, @"\b(reserv\w*|compr\w*)\s+(voo\w*|passage\w*)\b") || 
            (currentState.CurrentIntent == Intent.WaitingForFlightSelection && ExtractFlightNumber(message) != null))
            return Intent.BookFlight;
        
        // Se já está esperando seleção e a mensagem parece um número de voo
        if (currentState.CurrentIntent == Intent.WaitingForFlightSelection && Regex.IsMatch(message, @"\b([a-zA-Z]{2}\d{3,4})\b"))
            return Intent.BookFlight;

        // Se está esperando detalhes e a mensagem contém destino/data
        if (currentState.CurrentIntent == Intent.WaitingForFlightDetails && ExtractFlightSearchParams(message).IsValid)
             return Intent.SearchFlights;

        return Intent.None; // Nenhuma intenção clara ou contexto correspondente
    }

    private (bool IsValid, string? Origin, string? Destination, DateTime? Date) ExtractFlightSearchParams(string message)
    {
        // Padrão simplificado para origem (captura após "de" e antes de "para")
        var originMatch = Regex.Match(message, @"de\s+([A-Za-zÀ-ÿ\s]+?)(?:\s+para\s+|$)", RegexOptions.IgnoreCase);

        // Padrão simplificado para destino (captura após "para" e antes de "em")
        var destinationMatch = Regex.Match(message, @"para\s+([A-Za-zÀ-ÿ\s]+?)(?:\s+em\s+|$)", RegexOptions.IgnoreCase);

        // Padrão para data (mantido igual)
        var dateMatch = Regex.Match(message, @"em\s+(\d{1,2}/\d{1,2}/\d{4})|(\d{1,2}\s+de\s+[\p{L}]+\s+de\s+\d{4})");

        string? origin = originMatch.Success ? originMatch.Groups[1].Value.Trim() : null;
        string? destination = destinationMatch.Success ? destinationMatch.Groups[1].Value.Trim() : null;

        DateTime? date = null;
        if (dateMatch.Success)
        {
            if (DateTime.TryParseExact(dateMatch.Groups[1].Value, "dd/MM/yyyy",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out var parsedDate))
            {
                date = parsedDate;
            }
        }

        // Validação: precisa ter origem, destino e data
        bool isValid = !string.IsNullOrWhiteSpace(origin) &&
                      !string.IsNullOrWhiteSpace(destination) &&
                      date.HasValue;

        return (isValid, origin, destination, date);
    }

    private string? ExtractFlightNumber(string message)
    {
        // Tenta extrair um padrão de número de voo (ex: AZ101, TP2023)
        var match = Regex.Match(message.ToUpper(), @"\b([a-zA-Z]{2}\d{3,4})\b", RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value.ToUpperInvariant() : null;
    }
}

