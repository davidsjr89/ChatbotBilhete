namespace ChatbotApiNet9.Models;

public class Flight
{
    public required string FlightNumber { get; set; }
    public required string Origin { get; set; }
    public required string Destination { get; set; }
    public DateTime DepartureTime { get; set; }
    public DateTime ArrivalTime { get; set; }
    public decimal Price { get; set; }
    public required string Airline { get; set; }
}

