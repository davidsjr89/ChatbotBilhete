using ChatbotApiNet9.Interfaces;
using ChatbotApiNet9.Models;
using ChatbotApiNet9.Models.Enumerators;
using ChatbotApiNet9.Services;
using Microsoft.Extensions.Logging;
using Moq;
using System.Collections;
using System.Reflection;
using System.Text.Json;

namespace TestChatbotApiNet9.Tests
{
    public class ChatServiceTests
    {
        private readonly Mock<ILogger<ChatService>> _loggerMock;
        private readonly Mock<ITicketService> _ticketServiceMock;
        private readonly Mock<IAiService> _aiServiceMock;
        private readonly ChatService _chatService;

        public ChatServiceTests()
        {
            _loggerMock = new Mock<ILogger<ChatService>>();
            _ticketServiceMock = new Mock<ITicketService>();
            _aiServiceMock = new Mock<IAiService>();
            _chatService = new ChatService(_loggerMock.Object, _ticketServiceMock.Object, _aiServiceMock.Object);
            
            // Inicializar o dicionário de sessões com uma sessão vazia para garantir que não seja nulo
            var sessionId = Guid.NewGuid().ToString();
            var request = new ChatMessageRequest
            {
                UserId = "test_user",
                Message = "Inicializar",
                SessionId = sessionId
            };
            _chatService.ProcessMessageAsync(request).Wait();
        }

        [Fact]
        public async Task ProcessMessageAsync_Greeting_ReturnsGreetingResponse()
        {
            // Arrange
            var request = new ChatMessageRequest
            {
                UserId = "test_user",
                Message = "Olá",
                SessionId = Guid.NewGuid().ToString()
            };

            // Act
            var response = await _chatService.ProcessMessageAsync(request);

            // Assert
            Assert.Contains("Olá", response.Response);
            Assert.Contains("assistente de reservas", response.Response.ToLower());
        }

        [Fact]
        public async Task ProcessMessageAsync_Help_ReturnsHelpResponse()
        {
            // Arrange
            var request = new ChatMessageRequest
            {
                UserId = "test_user",
                Message = "Preciso de ajuda",
                SessionId = Guid.NewGuid().ToString()
            };

            // Act
            var response = await _chatService.ProcessMessageAsync(request);

            // Assert
            Assert.Contains("ajudar", response.Response.ToLower());
            Assert.Contains("buscar voo", response.Response.ToLower());
        }

        [Fact]
        public async Task ProcessMessageAsync_PassengerCount_ValidatesInput()
        {
            // Arrange
            var sessionId = Guid.NewGuid().ToString();
            
            // Inicializar o dicionário de sessões com uma sessão para este teste
            var initRequest = new ChatMessageRequest
            {
                UserId = "test_user",
                Message = "Inicializar",
                SessionId = sessionId
            };
            await _chatService.ProcessMessageAsync(initRequest);
            
            // Configurar o mock do serviço de tickets
            _ticketServiceMock.Setup(t => t.GetAvailableSeatsAsync("AZ101"))
                .ReturnsAsync(10);
                
            // Criar um objeto BookingContext para o estado da sessão
            var bookingContext = new BookingContext
            {
                FlightNumber = "AZ101",
                FlightDetails = new Flight
                {
                    FlightNumber = "AZ101",
                    Origin = "GRU",
                    Destination = "LIS",
                    DepartureTime = DateTime.UtcNow.AddDays(7),
                    ArrivalTime = DateTime.UtcNow.AddDays(7).AddHours(12),
                    Price = 1500.50m,
                    Airline = "Azul"
                }
            };

            // Obter o tipo correto de SessionState aninhado em ChatService
            Type sessionStateType = typeof(ChatService).GetNestedType("SessionState", BindingFlags.NonPublic);

            // Criar uma instância da SessionState correta
            object sessionState = Activator.CreateInstance(sessionStateType);

            // Preencher as propriedades via reflexão
            PropertyInfo currentIntentProp = sessionStateType.GetProperty("CurrentIntent", BindingFlags.Public | BindingFlags.Instance);
            currentIntentProp.SetValue(sessionState, Intent.WaitingForPassengerCount);

            PropertyInfo contextDataProp = sessionStateType.GetProperty("ContextData", BindingFlags.Public | BindingFlags.Instance);
            contextDataProp.SetValue(sessionState, JsonSerializer.Serialize(bookingContext));

            // Obter o campo _sessionStates
            FieldInfo sessionStatesField = typeof(ChatService).GetField("_sessionStates", BindingFlags.NonPublic | BindingFlags.Instance);
            var sessionStates = sessionStatesField.GetValue(_chatService) as IDictionary;

            // Se for nulo, criar novo dicionário com o tipo correto
            if (sessionStates == null)
            {
                // Obter tipo Dictionary<string, SessionState>
                Type dictType = typeof(Dictionary<,>).MakeGenericType(typeof(string), sessionStateType);
                sessionStates = (IDictionary)Activator.CreateInstance(dictType);
                sessionStatesField.SetValue(_chatService, sessionStates);
            }

            // Adicionar o estado ao dicionário
            sessionStates[sessionId] = sessionState;

            // Preparar a requisição com a quantidade de passageiros
            var request = new ChatMessageRequest
            {
                UserId = "test_user",
                Message = "3",
                SessionId = sessionId
            };

            // Act
            var response = await _chatService.ProcessMessageAsync(request);

            // Assert
            Assert.Contains("passageiro", response.Response.ToLower());
        }

        [Fact]
        public async Task ProcessMessageAsync_InvalidPassengerCount_ReturnsErrorMessage()
        {
            // Arrange
            var sessionId = Guid.NewGuid().ToString();
            
            // Inicializar o dicionário de sessões com uma sessão para este teste
            var initRequest = new ChatMessageRequest
            {
                UserId = "test_user",
                Message = "Inicializar",
                SessionId = sessionId
            };
            await _chatService.ProcessMessageAsync(initRequest);
            
            // Configurar o mock do serviço de tickets para retornar poucos assentos
            _ticketServiceMock.Setup(t => t.GetAvailableSeatsAsync("AZ101"))
                .ReturnsAsync(2);
                
            // Criar um objeto BookingContext para o estado da sessão
            var bookingContext = new BookingContext
            {
                FlightNumber = "AZ101",
                FlightDetails = new Flight
                {
                    FlightNumber = "AZ101",
                    Origin = "GRU",
                    Destination = "LIS",
                    DepartureTime = DateTime.UtcNow.AddDays(7),
                    ArrivalTime = DateTime.UtcNow.AddDays(7).AddHours(12),
                    Price = 1500.50m,
                    Airline = "Azul"
                }
            };

            // Obter o tipo correto de SessionState aninhado em ChatService
            Type sessionStateType = typeof(ChatService).GetNestedType("SessionState", BindingFlags.NonPublic);

            // Criar uma instância da SessionState correta
            object sessionState = Activator.CreateInstance(sessionStateType);

            // Preencher as propriedades via reflexão
            PropertyInfo currentIntentProp = sessionStateType.GetProperty("CurrentIntent", BindingFlags.Public | BindingFlags.Instance);
            currentIntentProp.SetValue(sessionState, Intent.WaitingForPassengerCount);

            PropertyInfo contextDataProp = sessionStateType.GetProperty("ContextData", BindingFlags.Public | BindingFlags.Instance);
            contextDataProp.SetValue(sessionState, JsonSerializer.Serialize(bookingContext));

            // Obter o campo _sessionStates
            FieldInfo sessionStatesField = typeof(ChatService).GetField("_sessionStates", BindingFlags.NonPublic | BindingFlags.Instance);
            var sessionStates = sessionStatesField.GetValue(_chatService) as IDictionary;

            // Se for nulo, criar novo dicionário com o tipo correto
            if (sessionStates == null)
            {
                // Obter tipo Dictionary<string, SessionState>
                Type dictType = typeof(Dictionary<,>).MakeGenericType(typeof(string), sessionStateType);
                sessionStates = (IDictionary)Activator.CreateInstance(dictType);
                sessionStatesField.SetValue(_chatService, sessionStates);
            }

            // Adicionar o estado ao dicionário
            sessionStates[sessionId] = sessionState;

            // Preparar a requisição com quantidade excessiva de passageiros
            var request = new ChatMessageRequest
            {
                UserId = "test_user",
                Message = "5",
                SessionId = sessionId
            };

            // Act
            var response = await _chatService.ProcessMessageAsync(request);

            // Assert
            Assert.Contains("desculpe", response.Response.ToLower());
            Assert.Contains("apenas", response.Response.ToLower());
        }
    }
    
    // Classes auxiliares para os testes
    public class BookingContext
    {
        public string FlightNumber { get; set; } = string.Empty;
        public Flight? FlightDetails { get; set; }
        public int PassengerCount { get; set; } = 0;
        public List<Passenger> Passengers { get; set; } = new List<Passenger>();
        public int CurrentPassengerIndex { get; set; } = 0;
        public PassengerDataCollectionStep CurrentStep { get; set; } = PassengerDataCollectionStep.None;
        public Passenger CurrentPassenger { get; set; } = new Passenger();
    }
    
    public class SessionState
    {
        public Intent CurrentIntent { get; set; } = Intent.None;
        public string? ContextData { get; set; }
        public override string ToString() => $"Intent: {CurrentIntent}, Context: {(string.IsNullOrEmpty(ContextData) ? "Empty" : "Present")}";
    }
}
