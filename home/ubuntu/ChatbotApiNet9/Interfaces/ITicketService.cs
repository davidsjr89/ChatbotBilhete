using ChatbotApiNet9.Models;

namespace ChatbotApiNet9.Interfaces;
public interface ITicketService
{
    Task<List<Flight>> SearchFlightsAsync(string origin, string destination, DateTime date);
    Task<bool> BookFlightAsync(string flightNumber, string userId, List<Passenger> passengers);
    Task<List<Flight>> SearchAllFlightsAsync();
    Task<int> GetAvailableSeatsAsync(string flightNumber);
}
