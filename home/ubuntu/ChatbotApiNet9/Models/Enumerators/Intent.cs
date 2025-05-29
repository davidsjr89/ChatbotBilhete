namespace ChatbotApiNet9.Models.Enumerators;

// --- Métodos Auxiliares --- 
public enum Intent
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
