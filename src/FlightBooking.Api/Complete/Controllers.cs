using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace FlightBooking.Api.Complete;
[ApiController, Route("api/flights")]
public sealed class FlightsController(SupplierGateway suppliers) : ControllerBase {
    [HttpGet("search")]
    public async Task<ActionResult<SearchResponse>> Search([FromQuery] SearchRequest request, CancellationToken ct) {
        Rules.Route(request.Origin, request.Destination, request.DepartureDate!.Value);
        return Ok(await suppliers.SearchAsync(request.Origin.ToUpperInvariant(), request.Destination.ToUpperInvariant(), request.DepartureDate.Value, ct));
    }
}
[Authorize(AuthenticationSchemes="Identity.Bearer"), ApiController, Route("api/bookings")]
public sealed class BookingsController(BookingManager manager) : ControllerBase {
    private string Owner => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    private bool Admin => User.IsInRole("Admin");
    [HttpPost]
    public async Task<ActionResult<BookingView>> Create(BookingRequest request, [FromHeader(Name="Idempotency-Key")] string? key, CancellationToken ct) {
        if (string.IsNullOrWhiteSpace(key) || key.Length > 100) throw new ApiError(400, "Provide an Idempotency-Key header (1–100 characters).");
        var (booking, replay) = await manager.CreateAsync(request, Owner, key, ct);
        if (replay) return Ok(BookingView.From(booking));
        return CreatedAtAction(nameof(Get), new { reference = booking.Id.ToString("N") }, BookingView.From(booking));
    }
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default) {
        page = Math.Max(page, 1); pageSize = Math.Clamp(pageSize, 1, 100);
        var query = manager.Query.Where(x => Admin || x.OwnerId == Owner);
        var count = await query.CountAsync(ct);
        var items = await query.OrderByDescending(x => x.CreatedAtUtc).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return Ok(new { total = count, page, pageSize, items = items.Select(BookingView.From) });
    }
    [HttpGet("{reference:guid}")]
    public async Task<ActionResult<BookingView>> Get(Guid reference, CancellationToken ct) => Ok(BookingView.From(await manager.FindAsync(reference, Owner, Admin, ct)));
    [HttpPut("{reference:guid}/contact")]
    public async Task<ActionResult<BookingView>> Contact(Guid reference, ContactRequest request, CancellationToken ct) {
        var booking = await manager.FindAsync(reference, Owner, Admin, ct);
        await manager.UpdateContactAsync(booking, request, ct); return Ok(BookingView.From(booking));
    }
    [HttpPost("{reference:guid}/{operation}")]
    public async Task<ActionResult<BookingView>> Act(Guid reference, string operation, ActionRequest request, CancellationToken ct) {
        if (operation is not ("confirm" or "ticket" or "cancel" or "refund")) throw new ApiError(404, "Unknown operation.");
        var booking = await manager.FindAsync(reference, Owner, Admin, ct);
        await manager.ActAsync(booking, operation, request.Version, ct); return Ok(BookingView.From(booking));
    }
    [HttpDelete("{reference:guid}")]
    public async Task<IActionResult> Delete(Guid reference, [FromHeader(Name="If-Match")] string? version, CancellationToken ct) {
        var booking = await manager.FindAsync(reference, Owner, Admin, ct);
        await manager.ActAsync(booking, "cancel", (version ?? "").Trim('"'), ct); return NoContent();
    }
}
[Authorize(AuthenticationSchemes="Identity.Bearer"), ApiController, Route("api/account")]
public sealed class AccountController(UserManager<IdentityUser> users) : ControllerBase {
    [HttpGet("me")]
    public async Task<IActionResult> Me() {
        var user = await users.GetUserAsync(User);
        if (user is null) return Unauthorized();
        var roles = await users.GetRolesAsync(user);
        return Ok(new { user.Id, user.Email, roles = roles.Count == 0 ? new[] { "Customer" } : roles.ToArray() });
    }
}
[Authorize(AuthenticationSchemes="Identity.Bearer", Roles="Admin"), ApiController, Route("api/admin")]
public sealed class AdminController(TravelDb db, UserManager<IdentityUser> users) : ControllerBase {
    [HttpGet("users")]
    public async Task<IActionResult> Users(CancellationToken ct) {
        var items = await users.Users.OrderBy(x => x.Email).Take(100).ToListAsync(ct);
        var results = new List<object>();
        foreach (var user in items) results.Add(new { user.Id, user.Email, roles = await users.GetRolesAsync(user) });
        return Ok(results);
    }
    [HttpPut("users/{id}/role")]
    public async Task<IActionResult> Role(string id, RoleRequest request) {
        if (id == User.FindFirstValue(ClaimTypes.NameIdentifier)) throw new ApiError(409, "You cannot change your own admin role.");
        var user = await users.FindByIdAsync(id) ?? throw new ApiError(404, "User not found.");
        await using var transaction = await db.Database.BeginTransactionAsync();
        var removed = await users.RemoveFromRolesAsync(user, await users.GetRolesAsync(user));
        if (!removed.Succeeded) throw new ApiError(409, "Role update failed.");
        var added = await users.AddToRoleAsync(user, request.Role);
        if (!added.Succeeded) throw new ApiError(409, "Role update failed.");
        await users.UpdateSecurityStampAsync(user);
        await transaction.CommitAsync();
        return Ok(new { message = "Role updated. The user's existing sessions have been invalidated; sign in again." });
    }
    [HttpGet("reports")]
    public async Task<IActionResult> Report(CancellationToken ct) {
        var groups = await db.Bookings.GroupBy(x => x.Status).Select(g => new { status = g.Key, count = g.Count(), total = g.Sum(x => x.TotalPrice) }).ToListAsync(ct);
        var revenue = await db.Bookings.Where(x => x.Status == BookingStatus.Ticketed).SumAsync(x => (decimal?)x.TotalPrice, ct) ?? 0;
        return Ok(new { groups, ticketedRevenue = revenue, currency = "BDT" });
    }
    [HttpGet("sql-summary")]
    public async Task<IActionResult> SqlSummary(CancellationToken ct) => Ok(await db.Database.SqlQueryRaw<BookingSummary>(
        "EXEC dbo.BookingSummary @FromUtc, @ToUtc",
        new Microsoft.Data.SqlClient.SqlParameter("@FromUtc", DateTimeOffset.UtcNow.AddYears(-10)),
        new Microsoft.Data.SqlClient.SqlParameter("@ToUtc", DateTimeOffset.UtcNow.AddDays(1))).ToListAsync(ct));
}
