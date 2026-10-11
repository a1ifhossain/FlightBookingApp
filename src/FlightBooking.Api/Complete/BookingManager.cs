using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
namespace FlightBooking.Api.Complete;
public sealed class BookingManager(TravelDb db, SupplierGateway suppliers) {
    public IQueryable<BookingEntity> Query => db.Bookings.Include(x => x.Passengers).Include(x => x.Tickets).Include(x => x.Events).AsSplitQuery();
    public async Task<BookingEntity> FindAsync(Guid id, string owner, bool admin, CancellationToken ct) => await Query.SingleOrDefaultAsync(x => x.Id == id && (admin || x.OwnerId == owner), ct) ?? throw new ApiError(404, "Booking not found.");
    public async Task<(BookingEntity booking, bool replay)> CreateAsync(BookingRequest request, string owner, string key, CancellationToken ct) {
        Rules.Route(request.Origin, request.Destination, request.DepartureDate!.Value);
        if (request.Passengers.Any(x => x is null || string.IsNullOrWhiteSpace(x.FullName) || x.FullName.Trim().Length < 2)) throw new ApiError(400, "Enter valid passenger names.");
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(request))));
        var previous = await Query.SingleOrDefaultAsync(x => x.OwnerId == owner && x.IdempotencyKey == key, ct);
        if (previous is not null) return Replay(previous, hash);
        var offers = await suppliers.Get(request.Supplier).SearchAsync(request.Origin.ToUpperInvariant(), request.Destination.ToUpperInvariant(), request.DepartureDate.Value, ct);
        var offer = offers.SingleOrDefault(x => x.Id == request.OfferId);
        if (offer is null || offer.AvailableSeats < request.Passengers.Count) throw new ApiError(409, "Offer unavailable or not enough seats. Search again.");
        var booking = new BookingEntity { Id = Guid.NewGuid(), OwnerId = owner, IdempotencyKey = key, RequestHash = hash, OfferId = offer.Id, Supplier = offer.Supplier, Airline = offer.Airline, Origin = offer.Origin, Destination = offer.Destination, DepartureDate = offer.DepartureDate, UnitPrice = offer.Price, TotalPrice = offer.Price * request.Passengers.Count, Currency = offer.Currency, ContactEmail = request.ContactEmail.Trim(), CreatedAtUtc = DateTimeOffset.UtcNow, Passengers = request.Passengers.Select(x => new PassengerEntity { Id = Guid.NewGuid(), FullName = x.FullName.Trim() }).ToList() };
        booking.Events.Add(new() { Action = "Created", AtUtc = DateTimeOffset.UtcNow });
        db.Bookings.Add(booking);
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 }) {
            db.ChangeTracker.Clear();
            var duplicate = await Query.SingleAsync(x => x.OwnerId == owner && x.IdempotencyKey == key, ct);
            return Replay(duplicate, hash);
        }
        return (booking, false);
    }
    private static (BookingEntity, bool) Replay(BookingEntity booking, string hash) {
        if (booking.RequestHash != hash) throw new ApiError(409, "Idempotency key already used for a different request.");
        return (booking, true);
    }
    public static void CheckVersion(BookingEntity booking, string version) {
        if (Convert.ToBase64String(booking.Version) != version) throw new ApiError(409, "Booking changed. Reload and try again.");
    }
    public async Task UpdateContactAsync(BookingEntity booking, ContactRequest request, CancellationToken ct) {
        CheckVersion(booking, request.Version);
        if (booking.Status is BookingStatus.Cancelled or BookingStatus.Refunded) throw new ApiError(409, "Cannot edit a closed booking.");
        booking.ContactEmail = request.ContactEmail.Trim();
        booking.Events.Add(new() { Action = "ContactUpdated", AtUtc = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync(ct);
    }
    public async Task ActAsync(BookingEntity booking, string action, string version, CancellationToken ct) {
        CheckVersion(booking, version);
        var next = Rules.Next(booking.Status, action, booking.PaymentReference != null);
        if (action is "confirm" or "ticket") Rules.Route(booking.Origin, booking.Destination, booking.DepartureDate);
        if (action != "cancel" || booking.Status != BookingStatus.Pending) {
            var result = await suppliers.Get(booking.Supplier).OperateAsync(action, booking.Id, ct);
            if (action == "confirm") booking.SupplierReference = result.Reference;
        }
        if (action == "ticket") {
            booking.PaymentReference = $"MOCK-PAY-{booking.Id:N}";
            foreach (var passenger in booking.Passengers) { var ticket = new TicketEntity { Id = Guid.NewGuid(), BookingId = booking.Id, PassengerId = passenger.Id, Number = $"MOCK-TKT-{passenger.Id:N}" }; db.Tickets.Add(ticket); }
        }
        booking.Status = next;
        booking.Events.Add(new() { Action = action, AtUtc = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync(ct);
    }
}
