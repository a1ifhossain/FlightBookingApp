using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
namespace FlightBooking.Api.Complete;
public enum BookingStatus { Pending, Confirmed, Ticketed, Cancelled, Refunded }
public sealed class BookingEntity {
    public Guid Id { get; set; }
    public string OwnerId { get; set; } = "";
    public IdentityUser Owner { get; set; } = null!;
    public string IdempotencyKey { get; set; } = "";
    public string RequestHash { get; set; } = "";
    public string OfferId { get; set; } = "";
    public string Supplier { get; set; } = "";
    public string Airline { get; set; } = "";
    public string Origin { get; set; } = "";
    public string Destination { get; set; } = "";
    public DateOnly DepartureDate { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public string Currency { get; set; } = "BDT";
    public string ContactEmail { get; set; } = "";
    public BookingStatus Status { get; set; }
    public string? SupplierReference { get; set; }
    public string? PaymentReference { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public byte[] Version { get; set; } = [];
    public List<PassengerEntity> Passengers { get; set; } = [];
    public List<TicketEntity> Tickets { get; set; } = [];
    public List<BookingEventEntity> Events { get; set; } = [];
}
public sealed class PassengerEntity {
    public Guid Id { get; set; }
    public Guid BookingId { get; set; }
    public BookingEntity Booking { get; set; } = null!;
    public string FullName { get; set; } = "";
}
public sealed class TicketEntity {
    public Guid Id { get; set; }
    public Guid BookingId { get; set; }
    public BookingEntity Booking { get; set; } = null!;
    public Guid PassengerId { get; set; }
    public PassengerEntity Passenger { get; set; } = null!;
    public string Number { get; set; } = "";
}
public sealed class BookingEventEntity {
    public long Id { get; set; }
    public Guid BookingId { get; set; }
    public BookingEntity Booking { get; set; } = null!;
    public string Action { get; set; } = "";
    public DateTimeOffset AtUtc { get; set; }
}
public sealed class TravelDb(DbContextOptions<TravelDb> options) : IdentityDbContext<IdentityUser>(options) {
    public DbSet<BookingEntity> Bookings => Set<BookingEntity>();
    public DbSet<PassengerEntity> Passengers => Set<PassengerEntity>();
    public DbSet<TicketEntity> Tickets => Set<TicketEntity>();
    public DbSet<BookingEventEntity> BookingEvents => Set<BookingEventEntity>();
    protected override void OnModelCreating(ModelBuilder b) {
        base.OnModelCreating(b);
        b.Entity<BookingEntity>(e => {
            e.HasKey(x => x.Id);
            e.Property(x => x.OwnerId).HasMaxLength(450);
            e.Property(x => x.IdempotencyKey).HasMaxLength(100);
            e.Property(x => x.RequestHash).HasMaxLength(64);
            e.Property(x => x.OfferId).HasMaxLength(50);
            e.Property(x => x.Supplier).HasMaxLength(10);
            e.Property(x => x.Airline).HasMaxLength(100);
            e.Property(x => x.Origin).HasMaxLength(3);
            e.Property(x => x.Destination).HasMaxLength(3);
            e.Property(x => x.Currency).HasMaxLength(3);
            e.Property(x => x.ContactEmail).HasMaxLength(254);
            e.Property(x => x.SupplierReference).HasMaxLength(80);
            e.Property(x => x.PaymentReference).HasMaxLength(80);
            e.Property(x => x.UnitPrice).HasPrecision(18,2);
            e.Property(x => x.TotalPrice).HasPrecision(18,2);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.Version).IsRowVersion();
            e.HasIndex(x => new { x.OwnerId, x.IdempotencyKey }).IsUnique();
            e.HasIndex(x => new { x.Origin, x.Destination, x.DepartureDate });
            e.HasIndex(x => new { x.OwnerId, x.CreatedAtUtc });
            e.HasOne(x => x.Owner).WithMany().HasForeignKey(x => x.OwnerId).OnDelete(DeleteBehavior.Restrict);
        });
        b.Entity<PassengerEntity>().Property(x => x.FullName).HasMaxLength(100);
        b.Entity<TicketEntity>().Property(x => x.Number).HasMaxLength(80);
        b.Entity<TicketEntity>().HasIndex(x => x.Number).IsUnique();
        b.Entity<TicketEntity>().HasOne(x => x.Passenger).WithMany().HasForeignKey(x => x.PassengerId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<BookingEventEntity>().Property(x => x.Action).HasMaxLength(30);
    }
}
