using FlightBooking.Api.Models;
namespace FlightBooking.Api.Services;

// The application depends on this contract, not on one airline's implementation.
public interface IFlightSupplier
{
    Task<IReadOnlyList<FlightOffer>> SearchAsync(string origin, string destination,
        DateOnly departureDate, CancellationToken cancellationToken);
}

public sealed class MockFlightSupplier : IFlightSupplier
{
    public Task<IReadOnlyList<FlightOffer>> SearchAsync(string origin, string destination,
        DateOnly departureDate, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        // Fictional offers: this is not live airline inventory or an HTTP integration.
        IReadOnlyList<FlightOffer> offers = new[]
        {
            new FlightOffer("MOCK-001", "Demo Air", "DAC", "CXB", departureDate, 6500m, "BDT"),
            new FlightOffer("MOCK-002", "Sample Airways", "DAC", "CXB", departureDate, 5800m, "BDT"),
            new FlightOffer("MOCK-003", "Demo Air", "DAC", "CGP", departureDate, 4200m, "BDT")
        };
        return Task.FromResult(offers);
    }
}

public sealed class FlightSearchService(IFlightSupplier supplier)
{
    public async Task<IReadOnlyList<FlightOffer>> SearchAsync(string origin, string destination,
        DateOnly departureDate, CancellationToken cancellationToken)
    {
        var offers = await supplier.SearchAsync(origin, destination, departureDate, cancellationToken);
        return offers.Where(f => f.Origin == origin && f.Destination == destination)
            .OrderBy(f => f.Price).ToArray();
    }
}
