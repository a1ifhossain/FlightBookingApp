using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
namespace FlightBooking.Api.Complete;
[DbContext(typeof(TravelDb))]
[Migration("20261009235959_ReportingProcedure")]
public sealed class ReportingMigration : Migration {
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
        CREATE OR ALTER PROCEDURE dbo.BookingSummary
            @FromUtc datetimeoffset,
            @ToUtc datetimeoffset
        AS
        BEGIN
            SET NOCOUNT ON;
            SELECT Status, COUNT(*) AS BookingCount, SUM(TotalPrice) AS BookingValue
            FROM dbo.Bookings
            WHERE CreatedAtUtc >= @FromUtc AND CreatedAtUtc < @ToUtc
            GROUP BY Status;
        END
        """);
    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.BookingSummary;");
}
public sealed class BookingSummary {
    public string Status { get; set; } = "";
    public int BookingCount { get; set; }
    public decimal BookingValue { get; set; }
}
