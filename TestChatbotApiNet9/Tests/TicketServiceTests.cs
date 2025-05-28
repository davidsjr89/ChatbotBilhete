using ChatbotApiNet9.Models;
using ChatbotApiNet9.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace TestChatbotApiNet9.Tests;

public class TicketServiceTests
{
    private readonly Mock<ILogger<SimulatedTicketService>> _loggerMock;
    private readonly SimulatedTicketService _ticketService;

    public TicketServiceTests()
    {
        _loggerMock = new Mock<ILogger<SimulatedTicketService>>();
        _ticketService = new SimulatedTicketService(_loggerMock.Object);
    }

    [Fact]
    public async Task SearchFlightsAsync_ValidParameters_ReturnsFlights()
    {
        // Arrange
        string origin = "SÃO PAULO";
        string destination = "RIO DE JANEIRO";
        DateTime date = new DateTime(2025, 5, 28);

        // Act
        var result = await _ticketService.SearchFlightsAsync(origin, destination, date);

        // Assert
        Assert.NotEmpty(result);
        Assert.Contains(result, f => f.FlightNumber == "GO34094");
    }

    [Fact]
    public async Task SearchFlightsAsync_InvalidParameters_ReturnsEmpty()
    {
        // Arrange
        string origin = "SÃO PAULO";
        string destination = "RIO DE JANEIRO";
        DateTime date = new DateTime(2026, 1, 1); // Data sem voo

        // Act
        var result = await _ticketService.SearchFlightsAsync(origin, destination, date);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAvailableSeatsAsync_ValidFlightNumber_ReturnsPositiveNumber()
    {
        // Arrange
        string flightNumber = "AZ101";

        // Act
        int availableSeats = await _ticketService.GetAvailableSeatsAsync(flightNumber);

        // Assert
        Assert.True(availableSeats > 0);
    }

    [Fact]
    public async Task GetAvailableSeatsAsync_InvalidFlightNumber_ReturnsZero()
    {
        // Arrange
        string flightNumber = "INVALID";

        // Act
        int availableSeats = await _ticketService.GetAvailableSeatsAsync(flightNumber);

        // Assert
        Assert.Equal(0, availableSeats);
    }

    [Fact]
    public async Task BookFlightAsync_ValidFlightWithPassengers_ReturnsTrue()
    {
        // Arrange
        string flightNumber = "AZ101";
        string userId = "test-user";
        var passengers = new List<Passenger>
            {
                new Passenger
                {
                    Name = "João Silva",
                    RG = "12345678",
                    CPF = "52998224725",
                    BirthDate = new DateTime(1990, 1, 1)
                },
                new Passenger
                {
                    Name = "Maria Souza",
                    RG = "87654321",
                    CPF = "98765432198",
                    BirthDate = new DateTime(1985, 5, 15)
                }
            };

        // Act
        bool result = await _ticketService.BookFlightAsync(flightNumber, userId, passengers);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task BookFlightAsync_InvalidFlight_ReturnsFalse()
    {
        // Arrange
        string flightNumber = "INVALID";
        string userId = "test-user";
        var passengers = new List<Passenger>
            {
                new Passenger
                {
                    Name = "João Silva",
                    RG = "12345678",
                    CPF = "52998224725",
                    BirthDate = new DateTime(1990, 1, 1)
                }
            };

        // Act
        bool result = await _ticketService.BookFlightAsync(flightNumber, userId, passengers);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task BookFlightAsync_TooManyPassengers_ReturnsFalse()
    {
        // Arrange
        string flightNumber = "AZ101";
        string userId = "test-user";

        // Criar uma lista com mais passageiros do que assentos disponíveis
        var passengers = new List<Passenger>();
        for (int i = 0; i < 200; i++) // Mais do que a capacidade do voo
        {
            passengers.Add(new Passenger
            {
                Name = $"Passageiro {i}",
                RG = "12345678",
                CPF = "52998224725",
                BirthDate = new DateTime(1990, 1, 1)
            });
        }

        // Act
        bool result = await _ticketService.BookFlightAsync(flightNumber, userId, passengers);

        // Assert
        Assert.False(result);
    }
}