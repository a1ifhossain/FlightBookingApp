using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
namespace FlightBooking.Api.Complete;
public sealed class DesignFactory : IDesignTimeDbContextFactory<TravelDb> {
    public TravelDb CreateDbContext(string[] args) => new(new DbContextOptionsBuilder<TravelDb>().UseSqlServer("Server=localhost;Database=FlightBooking;Trusted_Connection=True;TrustServerCertificate=True").Options);
}
