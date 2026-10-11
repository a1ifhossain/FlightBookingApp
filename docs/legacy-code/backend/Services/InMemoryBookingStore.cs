using System.Collections.Concurrent;
using FlightBooking.Api.Models;
namespace FlightBooking.Api.Services;

public interface IBookingStore
{
    void Add(Booking booking);
    Booking? Find(string reference);
}

// Shared across requests and safe for concurrent reads/writes. Lost on restart.
public sealed class InMemoryBookingStore : IBookingStore
{
    private readonly ConcurrentDictionary<string, Booking> bookings = new(StringComparer.OrdinalIgnoreCase);
    public void Add(Booking booking)
    {
        if (!bookings.TryAdd(booking.Reference, booking))
            throw new InvalidOperationException("Duplicate booking reference.");
    }
    public Booking? Find(string reference) => bookings.GetValueOrDefault(reference);
}
