-- Execute against FlightBooking using SSMS or scripts/sql-check.ps1.
-- WHERE + ORDER BY: upcoming active bookings.
SELECT TOP (20) Id, Origin, Destination, DepartureDate, TotalPrice, Status
FROM dbo.Bookings
WHERE Status IN ('Confirmed', 'Ticketed')
ORDER BY DepartureDate;
GO
-- JOIN: one row per passenger; booking totals repeat per passenger!
SELECT b.Id, u.Email AS OwnerEmail, p.FullName, b.TotalPrice
FROM dbo.Bookings b
JOIN dbo.AspNetUsers u ON u.Id = b.OwnerId
JOIN dbo.Passengers p ON p.BookingId = b.Id;
GO
-- LEFT JOIN: passengers who do not have tickets are retained.
SELECT p.FullName, t.Number
FROM dbo.Passengers p
LEFT JOIN dbo.Tickets t ON t.PassengerId = p.Id;
GO
-- GROUP BY: aggregate booking records, not passenger-joined rows.
SELECT Origin, Destination, COUNT(*) AS BookingCount,
       SUM(CASE WHEN Status = 'Ticketed' THEN TotalPrice ELSE 0 END) AS MockRevenue
FROM dbo.Bookings
GROUP BY Origin, Destination;
GO
-- Parameterized reporting procedure; not dynamic SQL.
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
END;
GO
EXEC dbo.BookingSummary @FromUtc='2020-01-01T00:00:00+00:00', @ToUtc='2100-01-01T00:00:00+00:00';
GO
-- EF migrations already create a composite route/date index and owner/date index.
-- Inspect indexes before adding another one.
SELECT name, is_unique FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.Bookings');
SET STATISTICS IO ON;
SELECT Id, TotalPrice FROM dbo.Bookings
WHERE Origin='DAC' AND Destination='CXB' AND DepartureDate='2026-12-20';
SET STATISTICS IO OFF;
GO
