using FlightBooking.Api.Contracts;
using FlightBooking.Api.Models;
using FlightBooking.Api.Services;
using Microsoft.AspNetCore.Mvc;
namespace FlightBooking.Api.Controllers;

[ApiController]
[Route("api/bookings")]
public sealed class BookingsController(BookingService bookings) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<Booking>> Create(CreateBookingRequest request, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(6)).DateTime);
        if (request.DepartureDate!.Value < today)
            return Problem(statusCode: 400, title: "Departure date cannot be in the past.");
        if (request.Origin.Equals(request.Destination, StringComparison.OrdinalIgnoreCase))
            return Problem(statusCode: 400, title: "Origin and destination must be different.");
        if (request.PassengerName.Trim().Length < 2)
            return Problem(statusCode: 400, title: "Passenger name must have at least two characters.");
        var booking = await bookings.CreateAsync(request, cancellationToken);
        if (booking is null)
            return Problem(statusCode: 409, title: "The offer is unavailable for this route. Search again.");
        return CreatedAtAction(nameof(Get), new { reference = booking.Reference }, booking);
    }

    [HttpGet("{reference}")]
    public ActionResult<Booking> Get(string reference)
    {
        var booking = bookings.Find(reference);
        return booking is null ? Problem(statusCode: 404, title: "Booking not found.") : Ok(booking);
    }
}
