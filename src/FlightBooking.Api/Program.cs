using FlightBooking.Api.Complete;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers().AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
var connection = builder.Configuration.GetConnectionString("SqlServer") ?? throw new InvalidOperationException("Set ConnectionStrings__SqlServer. See README Docker setup.");
builder.Services.AddDbContext<TravelDb>(o => o.UseSqlServer(connection));
builder.Services.AddIdentityApiEndpoints<IdentityUser>(o => { o.Password.RequiredLength = 10; o.User.RequireUniqueEmail = true; o.Lockout.MaxFailedAccessAttempts = 5; }).AddRoles<IdentityRole>().AddEntityFrameworkStores<TravelDb>();
builder.Services.AddAuthorization();
var origins = (builder.Configuration["Frontend:Origins"] ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
builder.Services.AddCors(o => o.AddPolicy("frontend", policy => {
    if (origins.Length > 0) policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod();
}));
builder.Services.Configure<Microsoft.AspNetCore.Authentication.BearerToken.BearerTokenOptions>(IdentityConstants.BearerScheme, o => o.BearerTokenExpiration = TimeSpan.FromMinutes(30));
var keyPath = builder.Configuration["DataProtection:Path"];
if (!string.IsNullOrWhiteSpace(keyPath)) builder.Services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(keyPath));
builder.Services.AddHttpClient<SupplierGateway>(client => { client.BaseAddress = new Uri(builder.Configuration["Supplier:Url"] ?? "http://127.0.0.1:5090"); client.Timeout = TimeSpan.FromSeconds(Math.Clamp(builder.Configuration.GetValue<int?>("Supplier:TimeoutSeconds") ?? 8, 1, 120)); });
builder.Services.AddScoped<BookingManager>();
builder.Services.AddRateLimiter(o => o.AddPolicy("auth", context => System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(context.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new() { PermitLimit = 30, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 })));
var app = builder.Build();
app.UseExceptionHandler();
app.UseCors("frontend");
app.UseRateLimiter();
app.UseAuthentication();
// Reject tokens after a password or role change updates the user's security stamp.
app.Use(async (context, next) => {
    if (context.User.Identity?.IsAuthenticated == true) {
        var users = context.RequestServices.GetRequiredService<UserManager<IdentityUser>>();
        var user = await users.GetUserAsync(context.User);
        var stampType = context.RequestServices.GetRequiredService<Microsoft.Extensions.Options.IOptions<IdentityOptions>>().Value.ClaimsIdentity.SecurityStampClaimType;
        if (user is null || context.User.FindFirst(stampType)?.Value != await users.GetSecurityStampAsync(user)) {
            context.Response.StatusCode = 401;
            return;
        }
    }
    await next(context);
});
app.UseAuthorization();
app.MapGroup("/api/auth").MapIdentityApi<IdentityUser>().RequireRateLimiting("auth");
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "Healthy" }));
app.MapGet("/health/ready", async (TravelDb db, CancellationToken ct) => await db.Database.CanConnectAsync(ct) ? Results.Ok(new { database = "Ready" }) : Results.StatusCode(503));
if (builder.Configuration.GetValue<bool>("Database:Initialize")) {
    await using var scope = app.Services.CreateAsyncScope();
    await scope.ServiceProvider.GetRequiredService<TravelDb>().Database.MigrateAsync();
    await Seed.RunAsync(scope.ServiceProvider, builder.Configuration);
}
app.Run();
public partial class Program { }
