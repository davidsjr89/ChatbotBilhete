using ChatbotApiNet9.Interfaces;
using ChatbotApiNet9.Models;
using System.Text.RegularExpressions;
using System.Text.Json;

namespace ChatbotApiNet9.Services;
public class ChatService : IChatService
{
    private readonly ILogger<ChatService> _logger;
    private readonly ITicketService _ticketService;
    private readonly IAiService _aiService;
    private readonly Dictionary<string, SessionState> _sessionStates = new Dictionary<string, SessionState>();

    public ChatService(ILogger<ChatService> logger, ITicketService ticketService, IAiService aiService)
    {
        _logger = logger;
        _ticketService = ticketService;
        _aiService = aiService;
    }

    public async Task<ChatMessageResponse> ProcessMessageAsync(ChatMessageRequest request)
    {
        var sessionId = request.SessionId ?? Guid.NewGuid().ToString();
        var message = request.Message;

        _logger.LogInformation("Processing message for session {SessionId}: {Message}", sessionId, message);

        // Recupera ou cria um novo estado de sessão
        if (!_sessionStates.TryGetValue(sessionId, out var currentState))
        {
            currentState = new SessionState();
            _sessionStates[sessionId] = currentState;
        }

        // Detecta a intenção com base na mensagem e no contexto atual
        var intent = DetectIntent(message, currentState);
        _logger.LogInformation("Detected intent: {Intent} for session {SessionId}", intent, sessionId);

        // Processa a mensagem com base na intenção detectada
        string responseMessage;
        object? actionData = null;

        switch (intent)
        {
            case Intent.Greeting:
                responseMessage = "Olá! Sou o assistente de reservas de voos. Como posso ajudar você hoje?";
                currentState.CurrentIntent = Intent.None;
                break;

            case Intent.Help:
                responseMessage = "Posso ajudar você a buscar voos e fazer reservas. Por exemplo, você pode dizer 'Buscar voo de São Paulo para Rio de Janeiro em 28/05/2025' ou 'Quero reservar um voo'.";
                currentState.CurrentIntent = Intent.None;
                break;

            case Intent.SearchFlights:
                (responseMessage, actionData, currentState) = await HandleFlightSearchAsync(message, currentState);
                break;

            case Intent.BookFlight:
                (responseMessage, actionData, currentState) = await HandleFlightBookingAsync(message, currentState);
                break;

            case Intent.WaitingForPassengerCount:
                (responseMessage, actionData, currentState) = await HandlePassengerCountAsync(message, currentState);
                break;

            case Intent.WaitingForPassengerDetails:
                (responseMessage, actionData, currentState) = await HandlePassengerDetailsAsync(message, currentState);
                break;

            case Intent.ConfirmReservation:
                (responseMessage, actionData, currentState) = await HandleReservationConfirmationAsync(message, currentState);
                break;

            default:
                // Fallback para processamento de linguagem natural
                responseMessage = await _aiService.GenerateResponseAsync(message);
                currentState.CurrentIntent = Intent.None;
                break;
        }

        _sessionStates[sessionId] = currentState; // Atualiza o estado da sessão
        var response = new ChatMessageResponse
        {
            Response = responseMessage,
            SessionId = sessionId,
            ActionData = actionData
        };
        _logger.LogInformation("Generated response: '{Response}' for session {SessionId}", response.Response, response.SessionId);
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
        public int PassengerCount { get; set; } = 0;
        public List<Passenger> Passengers { get; set; } = new List<Passenger>();
        public int CurrentPassengerIndex { get; set; } = 0;
        public PassengerDataCollectionStep CurrentStep { get; set; } = PassengerDataCollectionStep.None;
        public Passenger CurrentPassenger { get; set; } = new Passenger();
    }

    private enum PassengerDataCollectionStep
    {
        None,
        Name,
        RG,
        CPF,
        BirthDate,
        Complete
    }

    // --- Métodos Auxiliares --- 
    private enum Intent
    {
        None,
        Greeting,
        Help,
        SearchFlights,
        BookFlight,
        WaitingForFlightDetails,
        WaitingForFlightSelection,
        WaitingForPassengerCount,
        WaitingForPassengerDetails,
        ConfirmReservation,
        ConfirmFlightBooking
    }

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

        // Se estamos esperando confirmação de reserva
        if (currentState.CurrentIntent == Intent.ConfirmReservation &&
            (message.Contains("sim") || message.Contains("não") || message.Contains("nao")))
        {
            return Intent.ConfirmReservation;
        }

        // Se estamos esperando detalhes de passageiro
        if (currentState.CurrentIntent == Intent.WaitingForPassengerDetails)
        {
            return Intent.WaitingForPassengerDetails;
        }

        // Se estamos esperando quantidade de passageiros
        if (currentState.CurrentIntent == Intent.WaitingForPassengerCount &&
            Regex.IsMatch(message, @"\b\d+\b"))
        {
            return Intent.WaitingForPassengerCount;
        }

        // Detecção de intenções básicas
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

    private async Task<(string ResponseMessage, object? ActionData, SessionState NewState)> HandleFlightSearchAsync(string message, SessionState currentState)
    {
        var searchContext = new FlightSearchContext();

        // Se já estamos esperando detalhes de voo, tenta extrair os parâmetros
        if (currentState.CurrentIntent == Intent.WaitingForFlightDetails)
        {
            var searchParams = ExtractFlightSearchParams(message);
            if (searchParams.IsValid)
            {
                // Busca voos com os parâmetros extraídos
                var flights = await _ticketService.SearchFlightsAsync(
                    searchParams.Origin!,
                    searchParams.Destination!,
                    searchParams.Date!.Value);

                if (flights.Any())
                {
                    searchContext.Flights = flights;
                    searchContext.SearchParams = searchParams;

                    // Formata a resposta com os voos encontrados
                    var responseBuilder = new System.Text.StringBuilder();
                    responseBuilder.AppendLine($"Encontrei {flights.Count} voos de {searchParams.Origin} para {searchParams.Destination} em {searchParams.Date!.Value:dd/MM/yyyy}:");
                    
                    for (int i = 0; i < flights.Count; i++)
                    {
                        var flight = flights[i];
                        var availableSeats = await _ticketService.GetAvailableSeatsAsync(flight.FlightNumber);
                        responseBuilder.AppendLine($"{i + 1}. {flight.Airline} - Voo {flight.FlightNumber}");
                        responseBuilder.AppendLine($"   Partida: {flight.DepartureTime:dd/MM/yyyy HH:mm}");
                        responseBuilder.AppendLine($"   Chegada: {flight.ArrivalTime:dd/MM/yyyy HH:mm}");
                        responseBuilder.AppendLine($"   Preço: R$ {flight.Price:F2}");
                        responseBuilder.AppendLine($"   Assentos disponíveis: {availableSeats}");
                    }
                    
                    responseBuilder.AppendLine("\nPara reservar, digite o número do voo (ex: AZ101).");

                    // Salva o contexto e atualiza o estado
                    currentState.ContextData = JsonSerializer.Serialize(searchContext);
                    currentState.CurrentIntent = Intent.WaitingForFlightSelection;
                    
                    return (responseBuilder.ToString(), flights, currentState);
                }
                else
                {
                    // Nenhum voo encontrado
                    currentState.CurrentIntent = Intent.None;
                    return ("Não encontrei voos com esses critérios. Tente outras datas ou destinos.", null, currentState);
                }
            }
            else
            {
                // Parâmetros incompletos, pede mais informações
                currentState.CurrentIntent = Intent.WaitingForFlightDetails;
                return ("Por favor, forneça a origem, destino e data da viagem (ex: de São Paulo para Rio de Janeiro em 28/05/2025).", null, currentState);
            }
        }
        else
        {
            // Primeira interação de busca, verifica se já temos os parâmetros
            var searchParams = ExtractFlightSearchParams(message);
            if (searchParams.IsValid)
            {
                // Já temos os parâmetros, busca voos
                var flights = await _ticketService.SearchFlightsAsync(
                    searchParams.Origin!,
                    searchParams.Destination!,
                    searchParams.Date!.Value);

                if (flights.Any())
                {
                    searchContext.Flights = flights;
                    searchContext.SearchParams = searchParams;

                    // Formata a resposta com os voos encontrados
                    var responseBuilder = new System.Text.StringBuilder();
                    responseBuilder.AppendLine($"Encontrei {flights.Count} voos de {searchParams.Origin} para {searchParams.Destination} em {searchParams.Date!.Value:dd/MM/yyyy}:");
                    
                    for (int i = 0; i < flights.Count; i++)
                    {
                        var flight = flights[i];
                        var availableSeats = await _ticketService.GetAvailableSeatsAsync(flight.FlightNumber);
                        responseBuilder.AppendLine($"{i + 1}. {flight.Airline} - Voo {flight.FlightNumber}");
                        responseBuilder.AppendLine($"   Partida: {flight.DepartureTime:dd/MM/yyyy HH:mm}");
                        responseBuilder.AppendLine($"   Chegada: {flight.ArrivalTime:dd/MM/yyyy HH:mm}");
                        responseBuilder.AppendLine($"   Preço: R$ {flight.Price:F2}");
                        responseBuilder.AppendLine($"   Assentos disponíveis: {availableSeats}");
                    }
                    
                    responseBuilder.AppendLine("\nPara reservar, digite o número do voo (ex: AZ101).");

                    // Salva o contexto e atualiza o estado
                    currentState.ContextData = JsonSerializer.Serialize(searchContext);
                    currentState.CurrentIntent = Intent.WaitingForFlightSelection;
                    
                    return (responseBuilder.ToString(), flights, currentState);
                }
                else
                {
                    // Nenhum voo encontrado
                    currentState.CurrentIntent = Intent.None;
                    return ("Não encontrei voos com esses critérios. Tente outras datas ou destinos.", null, currentState);
                }
            }
            else
            {
                // Não temos os parâmetros, pede mais informações
                currentState.CurrentIntent = Intent.WaitingForFlightDetails;
                return ("Por favor, forneça a origem, destino e data da viagem (ex: de São Paulo para Rio de Janeiro em 28/05/2025).", null, currentState);
            }
        }
    }

    private async Task<(string ResponseMessage, object? ActionData, SessionState NewState)> HandleFlightBookingAsync(string message, SessionState currentState)
    {
        // Se já estamos esperando seleção de voo
        if (currentState.CurrentIntent == Intent.WaitingForFlightSelection)
        {
            // Tenta extrair o número do voo da mensagem
            string? flightNumber = ExtractFlightNumber(message);
            
            if (flightNumber != null)
            {
                // Verifica se o voo existe
                var flights = await _ticketService.SearchAllFlightsAsync();
                var selectedFlight = flights.FirstOrDefault(f => f.FlightNumber.Equals(flightNumber, StringComparison.OrdinalIgnoreCase));
                
                if (selectedFlight != null)
                {
                    // Verifica assentos disponíveis
                    int availableSeats = await _ticketService.GetAvailableSeatsAsync(flightNumber);
                    
                    if (availableSeats > 0)
                    {
                        // Cria contexto de reserva
                        var bookingContext = new BookingContext
                        {
                            FlightNumber = flightNumber,
                            FlightDetails = selectedFlight
                        };
                        
                        // Salva o contexto e atualiza o estado
                        currentState.ContextData = JsonSerializer.Serialize(bookingContext);
                        currentState.CurrentIntent = Intent.WaitingForPassengerCount;
                        
                        return ($"Você selecionou o voo {flightNumber} de {selectedFlight.Origin} para {selectedFlight.Destination} em {selectedFlight.DepartureTime:dd/MM/yyyy HH:mm}.\n\nQuantos passageiros deseja incluir nesta reserva? (Máximo: {availableSeats} assentos disponíveis)", selectedFlight, currentState);
                    }
                    else
                    {
                        // Sem assentos disponíveis
                        currentState.CurrentIntent = Intent.None;
                        return ($"Desculpe, não há assentos disponíveis para o voo {flightNumber}. Por favor, escolha outro voo.", null, currentState);
                    }
                }
                else
                {
                    // Voo não encontrado
                    currentState.CurrentIntent = Intent.WaitingForFlightSelection;
                    return ("Desculpe, não encontrei esse número de voo. Por favor, digite um número de voo válido (ex: AZ101).", null, currentState);
                }
            }
            else
            {
                // Número de voo inválido
                currentState.CurrentIntent = Intent.WaitingForFlightSelection;
                return ("Por favor, digite um número de voo válido (ex: AZ101).", null, currentState);
            }
        }
        else
        {
            // Primeira interação de reserva, pede para buscar voos primeiro
            currentState.CurrentIntent = Intent.WaitingForFlightDetails;
            return ("Para reservar um voo, primeiro preciso saber sua origem, destino e data. Por favor, forneça esses detalhes (ex: de São Paulo para Rio de Janeiro em 28/05/2025).", null, currentState);
        }
    }

    private async Task<(string ResponseMessage, object? ActionData, SessionState NewState)> HandlePassengerCountAsync(string message, SessionState currentState)
    {
        if (currentState.ContextData == null)
        {
            currentState.CurrentIntent = Intent.None;
            return ("Desculpe, ocorreu um erro ao processar sua reserva. Por favor, comece novamente.", null, currentState);
        }

        // Recupera o contexto de reserva
        var bookingContext = JsonSerializer.Deserialize<BookingContext>(currentState.ContextData);
        
        if (bookingContext == null)
        {
            currentState.CurrentIntent = Intent.None;
            return ("Desculpe, ocorreu um erro ao processar sua reserva. Por favor, comece novamente.", null, currentState);
        }

        // Tenta extrair o número de passageiros
        var match = Regex.Match(message, @"\b(\d+)\b");
        if (match.Success && int.TryParse(match.Groups[1].Value, out int passengerCount))
        {
            // Verifica se o número de passageiros é válido
            int availableSeats = await _ticketService.GetAvailableSeatsAsync(bookingContext.FlightNumber);
            
            if (passengerCount <= 0)
            {
                currentState.CurrentIntent = Intent.WaitingForPassengerCount;
                return ("O número de passageiros deve ser pelo menos 1. Por favor, informe quantos passageiros deseja incluir na reserva.", null, currentState);
            }
            else if (passengerCount > availableSeats)
            {
                currentState.CurrentIntent = Intent.WaitingForPassengerCount;
                return ($"Desculpe, há apenas {availableSeats} assentos disponíveis neste voo. Por favor, informe um número menor de passageiros.", null, currentState);
            }
            else
            {
                // Número de passageiros válido
                bookingContext.PassengerCount = passengerCount;
                bookingContext.CurrentPassengerIndex = 0;
                bookingContext.CurrentStep = PassengerDataCollectionStep.Name;
                bookingContext.CurrentPassenger = new Passenger();
                
                // Salva o contexto atualizado
                currentState.ContextData = JsonSerializer.Serialize(bookingContext);
                currentState.CurrentIntent = Intent.WaitingForPassengerDetails;
                
                return ($"Ótimo! Vamos coletar os dados dos {passengerCount} passageiros.\n\nPassageiro 1 de {passengerCount}:\nPor favor, informe o nome completo do passageiro:", null, currentState);
            }
        }
        else
        {
            // Não conseguiu extrair o número de passageiros
            currentState.CurrentIntent = Intent.WaitingForPassengerCount;
            return ("Por favor, informe apenas o número de passageiros que deseja incluir na reserva (ex: 2).", null, currentState);
        }
    }

    private async Task<(string ResponseMessage, object? ActionData, SessionState NewState)> HandlePassengerDetailsAsync(string message, SessionState currentState)
    {
        if (currentState.ContextData == null)
        {
            currentState.CurrentIntent = Intent.None;
            return ("Desculpe, ocorreu um erro ao processar sua reserva. Por favor, comece novamente.", null, currentState);
        }

        // Recupera o contexto de reserva
        var bookingContext = JsonSerializer.Deserialize<BookingContext>(currentState.ContextData);
        
        if (bookingContext == null)
        {
            currentState.CurrentIntent = Intent.None;
            return ("Desculpe, ocorreu um erro ao processar sua reserva. Por favor, comece novamente.", null, currentState);
        }

        // Processa a entrada com base no passo atual
        switch (bookingContext.CurrentStep)
        {
            case PassengerDataCollectionStep.Name:
                // Valida e salva o nome
                if (string.IsNullOrWhiteSpace(message) || message.Length < 3)
                {
                    return ("Por favor, informe um nome válido para o passageiro (nome completo):", null, currentState);
                }
                
                bookingContext.CurrentPassenger.Name = message.Trim();
                bookingContext.CurrentStep = PassengerDataCollectionStep.RG;
                
                currentState.ContextData = JsonSerializer.Serialize(bookingContext);
                return ($"Passageiro {bookingContext.CurrentPassengerIndex + 1} de {bookingContext.PassengerCount}:\nPor favor, informe o RG do passageiro (apenas números ou no formato XX.XXX.XXX-X):", null, currentState);

            case PassengerDataCollectionStep.RG:
                // Valida e salva o RG
                bookingContext.CurrentPassenger.RG = message.Trim();
                
                if (!bookingContext.CurrentPassenger.ValidateRG())
                {
                    return ("O RG informado não é válido. Por favor, informe um RG válido (apenas números ou no formato XX.XXX.XXX-X):", null, currentState);
                }
                
                bookingContext.CurrentStep = PassengerDataCollectionStep.CPF;
                
                currentState.ContextData = JsonSerializer.Serialize(bookingContext);
                return ($"Passageiro {bookingContext.CurrentPassengerIndex + 1} de {bookingContext.PassengerCount}:\nPor favor, informe o CPF do passageiro (apenas números ou no formato XXX.XXX.XXX-XX):", null, currentState);

            case PassengerDataCollectionStep.CPF:
                // Valida e salva o CPF
                bookingContext.CurrentPassenger.CPF = message.Trim();
                
                if (!bookingContext.CurrentPassenger.ValidateCPF())
                {
                    return ("O CPF informado não é válido. Por favor, informe um CPF válido (apenas números ou no formato XXX.XXX.XXX-XX):", null, currentState);
                }
                
                bookingContext.CurrentStep = PassengerDataCollectionStep.BirthDate;
                
                currentState.ContextData = JsonSerializer.Serialize(bookingContext);
                return ($"Passageiro {bookingContext.CurrentPassengerIndex + 1} de {bookingContext.PassengerCount}:\nPor favor, informe a data de nascimento do passageiro (formato DD/MM/AAAA):", null, currentState);

            case PassengerDataCollectionStep.BirthDate:
                // Valida e salva a data de nascimento
                if (!DateTime.TryParseExact(message.Trim(), "dd/MM/yyyy", 
                    System.Globalization.CultureInfo.InvariantCulture, 
                    System.Globalization.DateTimeStyles.None, out DateTime birthDate))
                {
                    return ("A data informada não é válida. Por favor, informe a data de nascimento no formato DD/MM/AAAA:", null, currentState);
                }
                
                bookingContext.CurrentPassenger.BirthDate = birthDate;
                
                if (!bookingContext.CurrentPassenger.ValidateBirthDate())
                {
                    return ("A data de nascimento informada não é válida (deve ser uma data no passado e a pessoa deve ter pelo menos 2 anos). Por favor, informe uma data válida:", null, currentState);
                }
                
                // Adiciona o passageiro à lista
                bookingContext.Passengers.Add(bookingContext.CurrentPassenger);
                bookingContext.CurrentPassengerIndex++;
                
                // Verifica se já coletamos dados de todos os passageiros
                if (bookingContext.CurrentPassengerIndex < bookingContext.PassengerCount)
                {
                    // Ainda há mais passageiros
                    bookingContext.CurrentStep = PassengerDataCollectionStep.Name;
                    bookingContext.CurrentPassenger = new Passenger();
                    
                    currentState.ContextData = JsonSerializer.Serialize(bookingContext);
                    return ($"Passageiro {bookingContext.CurrentPassengerIndex + 1} de {bookingContext.PassengerCount}:\nPor favor, informe o nome completo do passageiro:", null, currentState);
                }
                else
                {
                    // Todos os passageiros foram coletados, mostra resumo
                    bookingContext.CurrentStep = PassengerDataCollectionStep.Complete;
                    
                    var responseBuilder = new System.Text.StringBuilder();
                    responseBuilder.AppendLine("Resumo da reserva:");
                    responseBuilder.AppendLine($"Voo: {bookingContext.FlightNumber} - {bookingContext.FlightDetails?.Airline}");
                    responseBuilder.AppendLine($"De: {bookingContext.FlightDetails?.Origin} para {bookingContext.FlightDetails?.Destination}");
                    responseBuilder.AppendLine($"Data/Hora: {bookingContext.FlightDetails?.DepartureTime:dd/MM/yyyy HH:mm}");
                    responseBuilder.AppendLine($"Preço por passageiro: R$ {bookingContext.FlightDetails?.Price:F2}");
                    responseBuilder.AppendLine($"Preço total: R$ {bookingContext.FlightDetails?.Price * bookingContext.PassengerCount:F2}");
                    responseBuilder.AppendLine("\nPassageiros:");
                    
                    for (int i = 0; i < bookingContext.Passengers.Count; i++)
                    {
                        var passenger = bookingContext.Passengers[i];
                        responseBuilder.AppendLine($"{i + 1}. {passenger.Name}");
                        responseBuilder.AppendLine($"   RG: {passenger.RG}");
                        responseBuilder.AppendLine($"   CPF: {passenger.CPF}");
                        responseBuilder.AppendLine($"   Data de Nascimento: {passenger.BirthDate:dd/MM/yyyy}");
                    }
                    
                    responseBuilder.AppendLine("\nDeseja confirmar esta reserva? (sim/não)");
                    
                    currentState.ContextData = JsonSerializer.Serialize(bookingContext);
                    currentState.CurrentIntent = Intent.ConfirmReservation;
                    
                    return (responseBuilder.ToString(), null, currentState);
                }

            default:
                currentState.CurrentIntent = Intent.None;
                return ("Desculpe, ocorreu um erro ao processar sua reserva. Por favor, comece novamente.", null, currentState);
        }
    }

    private async Task<(string ResponseMessage, object? ActionData, SessionState NewState)> HandleReservationConfirmationAsync(string message, SessionState currentState)
    {
        if (currentState.ContextData == null)
        {
            currentState.CurrentIntent = Intent.None;
            return ("Desculpe, ocorreu um erro ao processar sua reserva. Por favor, comece novamente.", null, currentState);
        }

        // Recupera o contexto de reserva
        var bookingContext = JsonSerializer.Deserialize<BookingContext>(currentState.ContextData);
        
        if (bookingContext == null)
        {
            currentState.CurrentIntent = Intent.None;
            return ("Desculpe, ocorreu um erro ao processar sua reserva. Por favor, comece novamente.", null, currentState);
        }

        // Verifica a resposta do usuário
        if (message.ToLowerInvariant().Contains("sim"))
        {
            // Confirma a reserva
            string userId = Guid.NewGuid().ToString(); // Simula um ID de usuário
            bool success = await _ticketService.BookFlightAsync(bookingContext.FlightNumber, userId, bookingContext.Passengers);
            
            if (success)
            {
                // Reserva bem-sucedida
                currentState.CurrentIntent = Intent.None;
                currentState.ContextData = null;
                
                var reservation = new Reservation
                {
                    FlightNumber = bookingContext.FlightNumber,
                    FlightDetails = bookingContext.FlightDetails,
                    UserId = userId,
                    Passengers = bookingContext.Passengers,
                    IsConfirmed = true
                };
                
                return ($"Reserva confirmada com sucesso! Seu código de reserva é {reservation.ReservationId}.\n\nObrigado por utilizar nosso serviço de reservas. Tenha uma ótima viagem!", reservation, currentState);
            }
            else
            {
                // Falha na reserva
                currentState.CurrentIntent = Intent.None;
                return ("Desculpe, não foi possível confirmar sua reserva. Por favor, tente novamente mais tarde ou escolha outro voo.", null, currentState);
            }
        }
        else
        {
            // Cancelamento da reserva
            currentState.CurrentIntent = Intent.None;
            currentState.ContextData = null;
            return ("Reserva cancelada. Se desejar fazer uma nova reserva, estou à disposição.", null, currentState);
        }
    }
}
