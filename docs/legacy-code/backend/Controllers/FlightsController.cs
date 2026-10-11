using System.ComponentModel.DataAnnotations;
using FlightBooking.Api.Models;
using FlightBooking.Api.Services;
using Microsoft.AspNetCore.Mvc;
namespace FlightBooking.Api.Controllers;

public sealed class FlightSearchRequest
{
    [Required, RegularExpression("^[A-Za-z]{3}$")]
    public string Origin { get; set; } = "";
    [Required, RegularExpression("^[A-Za-z]{3}$")]
    public string Destination { get; set; } = "";
    [Required]
    public DateOnly? DepartureDate { get; set; }
}

[ApiController]
[Route("api/flights")]
public sealed class FlightsController(FlightSearchService searchService) : ControllerBase
{
    [HttpGet("search")]
    public async Task<ActionResult<IReadOnlyList<FlightOffer>>> Search(
        [FromQuery] FlightSearchRequest request, CancellationToken cancellationToken)
    {
        var origin = request.Origin.ToUpperInvariant();
        var destination = request.Destination.ToUpperInvariant();
        if (origin == destination)
            return Problem(statusCode: 400, title: "Origin and destination must be different.");
        var departureDate = request.DepartureDate!.Value;
        // Bangladesh uses UTC+6; avoid depending on the server's local timezone.
        var today = DateOnly.FromDateTime(DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(6)).DateTime);
        if (departureDate < today)
            return Problem(statusCode: 400, title: "Departure date cannot be in the past.");
        var offers = await searchService.SearchAsync(origin, destination, departureDate, cancellationToken);
        return Ok(offers);
    }
}
