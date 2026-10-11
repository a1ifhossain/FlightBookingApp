using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
namespace FlightBooking.Api.Complete;
public sealed class ApiExceptionHandler(IProblemDetailsService problems, ILogger<ApiExceptionHandler> logger) : IExceptionHandler {
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct) {
        var status = exception switch { ApiError e => e.Status, DbUpdateConcurrencyException => 409, HttpRequestException => 502, TaskCanceledException => 504, _ => 500 };
        var title = exception switch { ApiError e => e.Message, DbUpdateConcurrencyException => "Booking changed. Reload and try again.", HttpRequestException => "Supplier connection failed.", TaskCanceledException => "Supplier request timed out.", _ => "An unexpected error occurred." };
        logger.LogError("Request failed: {Type}; trace {Trace}", exception.GetType().Name, context.TraceIdentifier);
        context.Response.StatusCode = status;
        return await problems.TryWriteAsync(new() { HttpContext = context, ProblemDetails = new ProblemDetails { Status = status, Title = title, Extensions = { ["traceId"] = context.TraceIdentifier } } });
    }
}
public static class Seed {
    public static async Task RunAsync(IServiceProvider services, IConfiguration configuration) {
        var roles = services.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var name in new[] { "Customer", "Agent", "Admin" }) if (!await roles.RoleExistsAsync(name)) {
            var result = await roles.CreateAsync(new(name));
            if (!result.Succeeded) throw new InvalidOperationException("Role seed failed.");
        }
        var users = services.GetRequiredService<UserManager<IdentityUser>>();
        var password = configuration["Seed:Password"];
        if (string.IsNullOrWhiteSpace(password)) return;
        foreach (var (email, role) in new[] { ("admin@example.com", "Admin"), ("agent@example.com", "Agent"), ("customer@example.com", "Customer") }) {
            var user = await users.FindByEmailAsync(email);
            if (user is null) {
                user = new() { UserName = email, Email = email, EmailConfirmed = true };
                var result = await users.CreateAsync(user, password);
                if (!result.Succeeded) throw new InvalidOperationException("Seed account creation failed. Check password strength.");
            }
            if (!await users.IsInRoleAsync(user, role)) await users.AddToRoleAsync(user, role);
        }
    }
}
