using FlightBooking.Api.Complete;
using Xunit;
namespace FlightBooking.Tests;
public sealed class RulesTests {
    [Theory]
    [InlineData(BookingStatus.Pending, "confirm", false, BookingStatus.Confirmed)]
    [InlineData(BookingStatus.Confirmed, "ticket", false, BookingStatus.Ticketed)]
    [InlineData(BookingStatus.Ticketed, "cancel", true, BookingStatus.Cancelled)]
    [InlineData(BookingStatus.Cancelled, "refund", true, BookingStatus.Refunded)]
    public void AllowedTransitions(BookingStatus before, string action, bool paid, BookingStatus after) => Assert.Equal(after, Rules.Next(before, action, paid));
    [Theory]
    [InlineData(BookingStatus.Pending, "ticket", false)]
    [InlineData(BookingStatus.Cancelled, "refund", false)]
    [InlineData(BookingStatus.Refunded, "confirm", true)]
    [InlineData(BookingStatus.Ticketed, "ticket", true)]
    public void RejectIllegalTransitions(BookingStatus before, string action, bool paid) => Assert.Equal(409, Assert.Throws<ApiError>(() => Rules.Next(before, action, paid)).Status);
    [Fact] public void PastDateRejected() => Assert.Equal(400, Assert.Throws<ApiError>(() => Rules.Route("DAC", "CXB", Rules.Today.AddDays(-1))).Status);
    [Fact] public void SameAirportRejectedIgnoringCase() => Assert.Throws<ApiError>(() => Rules.Route("dac", "DAC", Rules.Today.AddDays(1)));
    [Fact] public void StaleVersionRejected() => Assert.Equal(409, Assert.Throws<ApiError>(() => BookingManager.CheckVersion(new() { Version = [1,2] }, "stale")).Status);
}
