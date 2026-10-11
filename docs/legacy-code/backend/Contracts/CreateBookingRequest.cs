using System.ComponentModel.DataAnnotations;
namespace FlightBooking.Api.Contracts;

public sealed class CreateBookingRequest
{
    [Required, StringLength(40)] public string OfferId { get; set; } = "";
    [Required, RegularExpression("^[A-Za-z]{3}$")] public string Origin { get; set; } = "";
    [Required, RegularExpression("^[A-Za-z]{3}$")] public string Destination { get; set; } = "";
    [Required] public DateOnly? DepartureDate { get; set; }
    [Required, StringLength(100, MinimumLength = 2)] public string PassengerName { get; set; } = "";
    [Required, EmailAddress, StringLength(254)] public string ContactEmail { get; set; } = "";
}
