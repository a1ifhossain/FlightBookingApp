using System.ComponentModel.DataAnnotations;
namespace FlightBooking.Api.Complete;
public sealed class SearchRequest {
    [Required, RegularExpression("^[A-Za-z]{3}$")] public string Origin { get; set; } = "";
    [Required, RegularExpression("^[A-Za-z]{3}$")] public string Destination { get; set; } = "";
    [Required] public DateOnly? DepartureDate { get; set; }
}
public sealed class PassengerRequest {
    [Required, StringLength(100, MinimumLength = 2)] public string FullName { get; set; } = "";
}
public sealed class BookingRequest {
    [Required, StringLength(50)] public string OfferId { get; set; } = "";
    [Required, RegularExpression("^(REST|SOAP)$")] public string Supplier { get; set; } = "";
    [Required, RegularExpression("^[A-Za-z]{3}$")] public string Origin { get; set; } = "";
    [Required, RegularExpression("^[A-Za-z]{3}$")] public string Destination { get; set; } = "";
    [Required] public DateOnly? DepartureDate { get; set; }
    [Required, EmailAddress, StringLength(254)] public string ContactEmail { get; set; } = "";
    [Required, MinLength(1), MaxLength(6)] public List<PassengerRequest> Passengers { get; set; } = [];
}
public sealed class ContactRequest {
    [Required, EmailAddress, StringLength(254)] public string ContactEmail { get; set; } = "";
    [Required] public string Version { get; set; } = "";
}
public sealed class ActionRequest { [Required] public string Version { get; set; } = ""; }
public sealed class RoleRequest { [Required, RegularExpression("^(Customer|Agent|Admin)$")] public string Role { get; set; } = ""; }
public sealed record Offer(string Id, string Supplier, string Airline, string Origin, string Destination, DateOnly DepartureDate, decimal Price, string Currency, int AvailableSeats);
public sealed record SearchResponse(IReadOnlyList<Offer> Offers, IReadOnlyList<string> Warnings);
public sealed record SupplierResult(string Reference);
public sealed record PassengerView(Guid Id, string FullName);
public sealed record TicketView(string Number, Guid PassengerId);
public sealed record EventView(string Action, DateTimeOffset AtUtc);
public sealed record BookingView(string Reference, string OwnerId, string Airline, string Origin, string Destination, DateOnly DepartureDate, decimal UnitPrice, decimal TotalPrice, string Currency, string ContactEmail, string Status, string? SupplierReference, string? PaymentReference, DateTimeOffset CreatedAtUtc, string Version, IReadOnlyList<PassengerView> Passengers, IReadOnlyList<TicketView> Tickets, IReadOnlyList<EventView> Events) {
    public static BookingView From(BookingEntity b) => new(b.Id.ToString("N"), b.OwnerId, b.Airline, b.Origin, b.Destination, b.DepartureDate, b.UnitPrice, b.TotalPrice, b.Currency, b.ContactEmail, b.Status.ToString(), b.SupplierReference, b.PaymentReference, b.CreatedAtUtc, Convert.ToBase64String(b.Version), b.Passengers.Select(x => new PassengerView(x.Id, x.FullName)).ToArray(), b.Tickets.Select(x => new TicketView(x.Number, x.PassengerId)).ToArray(), b.Events.OrderBy(x => x.AtUtc).Select(x => new EventView(x.Action, x.AtUtc)).ToArray());
}
public sealed class ApiError(int status, string message) : Exception(message) { public int Status { get; } = status; }
public static class Rules {
    public static DateOnly Today => DateOnly.FromDateTime(DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(6)).DateTime);
    public static void Route(string origin, string destination, DateOnly date) {
        if (date < Today) throw new ApiError(400, "Departure date cannot be in the past.");
        if (origin.Equals(destination, StringComparison.OrdinalIgnoreCase)) throw new ApiError(400, "Choose different airports.");
    }
    public static BookingStatus Next(BookingStatus status, string action, bool paid) => (status, action) switch {
        (BookingStatus.Pending, "confirm") => BookingStatus.Confirmed,
        (BookingStatus.Confirmed, "ticket") => BookingStatus.Ticketed,
        (BookingStatus.Pending or BookingStatus.Confirmed or BookingStatus.Ticketed, "cancel") => BookingStatus.Cancelled,
        (BookingStatus.Cancelled, "refund") when paid => BookingStatus.Refunded,
        _ => throw new ApiError(409, "This action is not allowed for the current booking state.")
    };
}
