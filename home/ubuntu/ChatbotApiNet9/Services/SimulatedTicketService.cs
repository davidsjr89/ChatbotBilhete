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
    new Flight {
        FlightNumber = "GO34094",
        Origin = "SÃO PAULO",
        Destination = "RIO DE JANEIRO",
        DepartureTime = new DateTime(2025, 5, 28),
        ArrivalTime = new DateTime(2025, 5, 28).AddHours(2),
        Price = 350.00m,
        Airline = "GOL"
    }
};

    // Capacidade de assentos por voo
    private static readonly Dictionary<string, int> _flightCapacity = new Dictionary<string, int>
    {
        { "AZ101", 180 },
        { "TP202", 220 },
        { "LA303", 150 },
        { "GO3404", 120 },
        { "GO34094", 120 }
    };

    // Reservas simuladas
    private static readonly Dictionary<string, List<Reservation>> _reservations = new Dictionary<string, List<Reservation>>();

    public SimulatedTicketService(ILogger<SimulatedTicketService> logger)
    {
        _logger = logger;
    }

    public Task<List<Flight>> SearchAllFlightsAsync()
    {
        return Task.FromResult(_flights);
    }

    public Task<List<Flight>> SearchFlightsAsync(string origin, string destination, DateTime date)
    {
        _logger.LogInformation("Simulating flight search from {Origin} to {Destination} on {Date}", origin, destination, date.Date);

        // Filtra os voos pela origem, destino e data (ignorando hora)
        var foundFlights = _flights.Where(f =>
            f.Origin.Equals(origin.ToUpper(), StringComparison.OrdinalIgnoreCase) &&
            f.Destination.Equals(destination.ToUpper(), StringComparison.OrdinalIgnoreCase) &&
            f.DepartureTime.Date == date.Date
        ).ToList();

        _logger.LogInformation("Found {Count} flights matching criteria.", foundFlights.Count);
        return Task.FromResult(foundFlights);
    }

    public Task<bool> BookFlightAsync(string flightNumber, string userId, List<Passenger> passengers)
    {
        _logger.LogInformation("Simulating booking flight {FlightNumber} for user {UserId} with {PassengerCount} passengers",
            flightNumber, userId, passengers.Count);

        // Verifica se o voo existe
        var flight = _flights.FirstOrDefault(f => f.FlightNumber.Equals(flightNumber, StringComparison.OrdinalIgnoreCase));
        if (flight == null)
        {
            _logger.LogWarning("Attempted to book non-existent flight {FlightNumber}.", flightNumber);
            return Task.FromResult(false);
        }

        // Verifica se há assentos disponíveis
        int availableSeats = GetAvailableSeatsAsync(flightNumber).Result;
        if (availableSeats < passengers.Count)
        {
            _logger.LogWarning("Not enough seats available for flight {FlightNumber}. Requested: {Requested}, Available: {Available}",
                flightNumber, passengers.Count, availableSeats);
            return Task.FromResult(false);
        }

        // Cria a reserva
        var reservation = new Reservation
        {
            FlightNumber = flightNumber,
            FlightDetails = flight,
            UserId = userId,
            Passengers = passengers,
            IsConfirmed = true
        };

        // Adiciona a reserva ao dicionário de reservas
        if (!_reservations.ContainsKey(flightNumber))
        {
            _reservations[flightNumber] = new List<Reservation>();
        }
        _reservations[flightNumber].Add(reservation);

        _logger.LogInformation("Flight {FlightNumber} booked successfully for {PassengerCount} passengers.",
            flightNumber, passengers.Count);
        return Task.FromResult(true);
    }

    public Task<int> GetAvailableSeatsAsync(string flightNumber)
    {
        // Verifica se o voo existe
        if (!_flightCapacity.ContainsKey(flightNumber))
        {
            _logger.LogWarning("Attempted to check seats for non-existent flight {FlightNumber}.", flightNumber);
            return Task.FromResult(0);
        }

        // Calcula assentos disponíveis
        int capacity = _flightCapacity[flightNumber];
        int reserved = 0;

        if (_reservations.ContainsKey(flightNumber))
        {
            reserved = _reservations[flightNumber].Sum(r => r.Passengers.Count);
        }

        int available = capacity - reserved;
        _logger.LogInformation("Flight {FlightNumber} has {Available} seats available out of {Capacity}.",
            flightNumber, available, capacity);

        return Task.FromResult(available);
    }

    // Método legado para compatibilidade
    public Task<bool> BookFlightAsync(string flightNumber, string userId)
    {
        // Cria um passageiro padrão para manter compatibilidade
        var passenger = new Passenger
        {
            Name = "Passageiro Padrão",
            RG = "00000000",
            CPF = "00000000000",
            BirthDate = DateTime.Now.AddYears(-30)
        };

        return BookFlightAsync(flightNumber, userId, new List<Passenger> { passenger });
    }
}
