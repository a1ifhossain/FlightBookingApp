namespace FlightBooking.Api.Models;

// Immutable snapshot: later the database will persist these values.
public sealed record Booking(string Reference, FlightOffer Flight, string PassengerName,
    string ContactEmail, string Status, DateTimeOffset CreatedAtUtc);
