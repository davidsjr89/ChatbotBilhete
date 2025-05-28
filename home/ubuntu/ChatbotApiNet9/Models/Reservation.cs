using System.Collections.Generic;

namespace ChatbotApiNet9.Models;

public class Reservation
{
    public string ReservationId { get; set; } = Guid.NewGuid().ToString();
    public string FlightNumber { get; set; } = string.Empty;
    public Flight? FlightDetails { get; set; }
    public string UserId { get; set; } = string.Empty;
    public List<Passenger> Passengers { get; set; } = new List<Passenger>();
    public DateTime ReservationDate { get; set; } = DateTime.UtcNow;
    public bool IsConfirmed { get; set; } = false;
}
