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

        string responseMessage = "";
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
                        actionData = flights;
                        currentState.CurrentIntent = Intent.WaitingForFlightSelection;
                        currentState.ContextData = JsonSerializer.Serialize(new FlightSearchContext
                        {
                            Flights = flights,
                            SearchParams = searchParams
                        });
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

            case Intent.ConfirmFlightBooking:
                if (request.Message.Equals("sim", StringComparison.OrdinalIgnoreCase))
                {
                    if (currentState.ContextData != null)
                    {
                        try
                        {
                            var bookingContext = JsonSerializer.Deserialize<BookingContext>(currentState.ContextData);
                            if (bookingContext != null)
                            {
                                bool booked = await _ticketService.BookFlightAsync(bookingContext.FlightNumber, request.UserId);
                                if (booked)
                                {
                                    responseMessage = $"Reserva confirmada! Seu voo {bookingContext.FlightNumber} foi reservado com sucesso.";
                                }
                                else
                                {
                                    responseMessage = "Não foi possível completar a reserva. Por favor, tente novamente.";
                                }
                            }
                        }
                        catch (JsonException ex)
                        {
                            _logger.LogError(ex, "Erro ao desserializar contexto de reserva");
                            responseMessage = "Ocorreu um erro ao processar sua reserva.";
                        }
                    }
                    currentState.CurrentIntent = Intent.None;
                    currentState.ContextData = null;
                }
                else if (request.Message.Equals("não", StringComparison.OrdinalIgnoreCase) ||
                         request.Message.Equals("nao", StringComparison.OrdinalIgnoreCase))
                {
                    responseMessage = "Reserva cancelada. Posso ajudar com algo mais?";
                    currentState.CurrentIntent = Intent.None;
                    currentState.ContextData = null;
                }
                else
                {
                    responseMessage = "Por favor, responda 'sim' para confirmar ou 'não' para cancelar a reserva.";
                }
                break;

            case Intent.BookFlight:
                var flightNumberToBook = ExtractFlightNumber(request.Message);
                if (!string.IsNullOrEmpty(flightNumberToBook))
                {
                    // Tenta encontrar o voo em três lugares:
                    // 1. No contexto da pesquisa anterior
                    // 2. Na lista geral de voos simulados
                    // 3. Se não encontrar, pede para o usuário fazer uma busca

                    Flight? flightToBook = null;
                    FlightSearchContext? searchContext = null;

                    // Verifica se há contexto de pesquisa anterior
                    if (currentState.ContextData != null)
                    {
                        try
                        {
                            searchContext = JsonSerializer.Deserialize<FlightSearchContext>(currentState.ContextData);
                            flightToBook = searchContext?.Flights?.FirstOrDefault(f =>
                                f.FlightNumber.Equals(flightNumberToBook, StringComparison.OrdinalIgnoreCase));
                        }
                        catch (JsonException ex)
                        {
                            _logger.LogWarning(ex, "Failed to deserialize flight context for session {SessionId}", sessionId);
                        }
                    }

                    // Se não encontrou no contexto, busca na lista geral de voos
                    if (flightToBook == null)
                    {
                        var allFlights = await _ticketService.SearchAllFlightsAsync();
                        flightToBook = allFlights.FirstOrDefault(f =>
                            f.FlightNumber.Equals(flightNumberToBook, StringComparison.OrdinalIgnoreCase));

                        if (flightToBook != null)
                        {
                            responseMessage = $"Encontrei o voo {flightNumberToBook} ({flightToBook.Origin} → {flightToBook.Destination}). Deseja reservar este voo? (responda 'sim' ou 'não')";
                            currentState.CurrentIntent = Intent.ConfirmFlightBooking;
                            currentState.ContextData = JsonSerializer.Serialize(new BookingContext
                            {
                                FlightNumber = flightNumberToBook,
                                FlightDetails = flightToBook
                            });
                            actionData = flightToBook;
                            break;
                        }
                    }

                    // Se encontrou o voo (no contexto ou na lista geral)
                    if (flightToBook != null)
                    {
                        bool booked = await _ticketService.BookFlightAsync(flightNumberToBook, request.UserId);
                        if (booked)
                        {
                            responseMessage = $"Ótimo! Seu voo {flightNumberToBook} foi reservado com sucesso.";
                            currentState.CurrentIntent = Intent.None;
                            currentState.ContextData = null;
                        }
                        else
                        {
                            responseMessage = $"Houve um problema ao tentar reservar o voo {flightNumberToBook}. Por favor, tente novamente mais tarde.";
                        }
                    }
                    else
                    {
                        responseMessage = $"Não encontrei o voo {flightNumberToBook}. Por favor, faça uma pesquisa primeiro (ex: 'buscar voo para Rio de Janeiro') ou verifique o número do voo.";
                        currentState.CurrentIntent = Intent.None;
                        currentState.ContextData = null;
                    }
                }
                else
                {
                    responseMessage = "Por favor, informe o número do voo que deseja reservar (ex: 'reservar voo AZ101').";
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

    private class FlightSearchContext
    {
        public List<Flight> Flights { get; set; } = new List<Flight>();
        public (bool IsValid, string? Origin, string? Destination, DateTime? Date) SearchParams { get; set; }
    }

    private class BookingContext
    {
        public string FlightNumber { get; set; } = string.Empty;
        public Flight? FlightDetails { get; set; }
    }

    // --- Métodos Auxiliares --- 

    private enum Intent { None, Greeting, Help, SearchFlights, BookFlight, WaitingForFlightDetails, WaitingForFlightSelection, ConfirmFlightBooking }

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

        // Se estivermos esperando confirmação e o usuário respondeu "sim" ou "não"
        if (currentState.CurrentIntent == Intent.ConfirmFlightBooking &&
            (message == "sim" || message == "não" || message == "nao"))
        {
            return Intent.ConfirmFlightBooking;
        }

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

