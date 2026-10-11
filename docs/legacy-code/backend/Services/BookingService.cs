using FlightBooking.Api.Contracts;
using FlightBooking.Api.Models;
namespace FlightBooking.Api.Services;

public sealed class BookingService(FlightSearchService search, IBookingStore store)
{
    public async Task<Booking?> CreateAsync(CreateBookingRequest request, CancellationToken cancellationToken)
    {
        var offers = await search.SearchAsync(request.Origin.ToUpperInvariant(),
            request.Destination.ToUpperInvariant(), request.DepartureDate!.Value, cancellationToken);
        var offer = offers.SingleOrDefault(f => f.Id == request.OfferId);
        if (offer is null) return null;
        cancellationToken.ThrowIfCancellationRequested();
        var booking = new Booking(Guid.NewGuid().ToString("N"), offer,
            request.PassengerName.Trim(), request.ContactEmail.Trim(), "Pending", DateTimeOffset.UtcNow);
        store.Add(booking);
        return booking;
    }
    public Booking? Find(string reference) => store.Find(reference);
}
