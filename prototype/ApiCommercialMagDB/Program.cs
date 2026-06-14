using System.Data;
using System.Security.Claims;
using CommercialMagDb.Api.Contracts.Auth;
using CommercialMagDb.Api.Contracts.Dashboard;
using CommercialMagDb.Api.Contracts.Sync;
using CommercialMagDb.Api.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddAuthorization();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwt = builder.Configuration.GetSection("Jwt").Get<JwtOptions>() ?? new JwtOptions();
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = JwtTokenService.BuildSigningKey(jwt.SigningKey),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyHeader()
              .AllowAnyMethod()
              .AllowAnyOrigin());
});

builder.Services.AddSingleton<JwtTokenService>();
builder.Services.AddSingleton<RefreshTokenStore>();
builder.Services.AddSingleton<PasswordHasher>();
builder.Services.AddScoped<DbConnectionFactory>();
builder.Services.AddScoped<AuthRepository>();
builder.Services.AddScoped<SyncRepository>();
builder.Services.AddScoped<DashboardRepository>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<SyncService>();
builder.Services.AddScoped<DashboardService>();

var app = builder.Build();

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

using (var scope = app.Services.CreateScope())
{
    var authService = scope.ServiceProvider.GetRequiredService<AuthService>();
    await authService.EnsureDevAdminAsync();
}

app.MapGet("/", () => Results.Ok(new { service = "CommercialMagDb.Api", status = "running" }));

var auth = app.MapGroup("/api/auth");
auth.MapPost("/login", async (LoginRequest request, AuthService service) =>
{
    var result = await service.LoginAsync(request.Username, request.Password);
    return result is null ? Results.Unauthorized() : Results.Ok(result);
});
auth.MapPost("/refresh", async (RefreshRequest request, AuthService service) =>
{
    var result = await service.RefreshAsync(request.RefreshToken);
    return result is null ? Results.Unauthorized() : Results.Ok(result);
});
auth.MapPost("/logout", async (RefreshRequest request, AuthService service) =>
{
    await service.LogoutAsync(request.RefreshToken);
    return Results.Ok();
}).RequireAuthorization();
auth.MapGet("/me", (ClaimsPrincipal user) =>
{
    return Results.Ok(new
    {
        userId = user.FindFirstValue(ClaimTypes.NameIdentifier),
        username = user.FindFirstValue(ClaimTypes.Name),
        role = user.FindFirstValue(ClaimTypes.Role)
    });
}).RequireAuthorization();

var stock = app.MapGroup("/api/stocksortie").RequireAuthorization();
stock.MapPost("", async (StockSortieSyncRequest request, SyncService service) =>
{
    var result = await service.SyncStockSortieAsync(request);
    return Results.Ok(result);
});

var depenses = app.MapGroup("/api/depenses").RequireAuthorization();
depenses.MapPost("", async (DepenseSyncRequest request, SyncService service) =>
{
    var result = await service.SyncDepenseAsync(request);
    return Results.Ok(result);
});

var dashboard = app.MapGroup("/api/dashboard").RequireAuthorization();
dashboard.MapGet("/journalier", async (DateTime? date, DashboardService service) =>
{
    var result = await service.GetJournalierAsync(date ?? DateTime.Today);
    return Results.Ok(result);
});
dashboard.MapGet("/mensuel", async (int? year, int? month, DashboardService service) =>
{
    var now = DateTime.Today;
    var result = await service.GetMensuelAsync(year ?? now.Year, month ?? now.Month);
    return Results.Ok(result);
});
dashboard.MapGet("/annuel", async (int? year, DashboardService service) =>
{
    var now = DateTime.Today;
    var result = await service.GetAnnuelAsync(year ?? now.Year);
    return Results.Ok(result);
});
dashboard.MapGet("/analyse-vente", async (string? periode, int? year, int? month, DateTime? date, DateTime? start, DateTime? end, DashboardService service) =>
{
    var today = date ?? DateTime.Today;
    var selectedYear = year ?? today.Year;
    var selectedMonth = month ?? today.Month;
    var mode = (periode ?? "mois").Trim().ToLowerInvariant();

    DateTime dateDebut;
    DateTime dateFin;

    if (start.HasValue && end.HasValue)
    {
        dateDebut = start.Value.Date;
        dateFin = end.Value.Date;
    }
    else if (mode == "annee")
    {
        dateDebut = new DateTime(selectedYear, 1, 1);
        dateFin = new DateTime(selectedYear, 12, 31);
    }
    else if (mode == "jour")
    {
        dateDebut = today.Date;
        dateFin = today.Date;
    }
    else
    {
        dateDebut = new DateTime(selectedYear, selectedMonth, 1);
        dateFin = new DateTime(selectedYear, selectedMonth, DateTime.DaysInMonth(selectedYear, selectedMonth));
    }

    var result = await service.GetAnalyseVenteAsync(dateDebut, dateFin);
    return Results.Ok(result);
});

app.Run();
