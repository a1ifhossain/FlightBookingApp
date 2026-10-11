namespace FlightBooking.Api.Models;

// A record groups related values and supplies value-based equality.
public sealed record FlightOffer(string Id, string Airline, string Origin,
    string Destination, DateOnly DepartureDate, decimal Price, string Currency);
