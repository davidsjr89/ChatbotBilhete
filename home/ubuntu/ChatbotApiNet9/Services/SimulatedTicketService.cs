using ChatbotApiNet9.Interfaces;
using ChatbotApiNet9.Models;

namespace ChatbotApiNet9.Services;

public class SimulatedTicketService : ITicketService
{
    private readonly ILogger<SimulatedTicketService> _logger;

    // Dados simulados de voos
    private static readonly List<Flight> _flights = new List<Flight>
    {
        new Flight { FlightNumber = "AZ101", Origin = "GRU", Destination = "LIS", DepartureTime = DateTime.UtcNow.AddDays(7).AddHours(10), ArrivalTime = DateTime.UtcNow.AddDays(7).AddHours(22), Price = 1500.50m, Airline = "Azul" },
        new Flight { FlightNumber = "TP202", Origin = "GRU", Destination = "LIS", DepartureTime = DateTime.UtcNow.AddDays(7).AddHours(14), ArrivalTime = DateTime.UtcNow.AddDays(8).AddHours(2), Price = 1650.00m, Airline = "TAP" },
        new Flight { FlightNumber = "LA303", Origin = "GRU", Destination = "SCL", DepartureTime = DateTime.UtcNow.AddDays(10).AddHours(8), ArrivalTime = DateTime.UtcNow.AddDays(10).AddHours(12), Price = 800.75m, Airline = "LATAM" },
        new Flight { FlightNumber = "GO3404", Origin = "CGH", Destination = "SDU", DepartureTime = DateTime.UtcNow.AddHours(9), ArrivalTime = DateTime.UtcNow.AddDays(3).AddHours(10), Price = 350.00m, Airline = "GOL" },
        new Flight { FlightNumber = "GO34094", Origin = "SÃO PAULO", Destination = "RIO DE JANEIRO", DepartureTime = DateTime.Parse("05/28/2025"), ArrivalTime = DateTime.UtcNow, Price = 350.00m, Airline = "GOL" },
    };

    public SimulatedTicketService(ILogger<SimulatedTicketService> logger)
    {
        _logger = logger;
    }

    public Task<List<Flight>> SearchFlightsAsync(string origin, string destination, DateTime date)
    {
        _logger.LogInformation("Simulating flight search from {Origin} to {Destination} on {Date}", origin, destination, date.Date);

        // Simula a busca filtrando os voos pela origem, destino e data (ignorando hora por simplicidade)
        var foundFlights = _flights.Where(f => 
            f.Origin.Equals(origin.ToUpper(), StringComparison.OrdinalIgnoreCase) &&
            f.Destination.Equals(destination.ToUpper(), StringComparison.OrdinalIgnoreCase) &&
            f.DepartureTime.Date == DateTime.Parse(date.Date.ToString("MM/dd/yyyy"))
        ).ToList();

        _logger.LogInformation("Found {Count} flights matching criteria.", foundFlights.Count);

        return Task.FromResult(foundFlights);
    }

    public Task<bool> BookFlightAsync(string flightNumber, string userId)
    {
        _logger.LogInformation("Simulating booking flight {FlightNumber} for user {UserId}", flightNumber, userId);

        // Simula a reserva - em um sistema real, isso envolveria interações complexas
        var flightExists = _flights.Any(f => f.FlightNumber.Equals(flightNumber, StringComparison.OrdinalIgnoreCase));

        if (flightExists)
        {
            _logger.LogInformation("Flight {FlightNumber} booked successfully (simulated).", flightNumber);
            return Task.FromResult(true); // Simula sucesso
        }
        else
        {
            _logger.LogWarning("Attempted to book non-existent flight {FlightNumber}.", flightNumber);
            return Task.FromResult(false); // Simula falha (voo não encontrado)
        }
    }
}

