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
ApiStartupLogger.Write(builder.Environment.ContentRootPath, $"Startup begin env={builder.Environment.EnvironmentName}");
var jwtOptions = builder.Configuration.GetSection("Jwt").Get<JwtOptions>() ?? new JwtOptions();
try
{
    _ = JwtTokenService.BuildSigningKey(jwtOptions.SigningKey);
}
catch (Exception ex)
{
    ApiStartupLogger.Write(builder.Environment.ContentRootPath, "Startup failed: invalid JWT configuration.", ex);
    throw;
}

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("DashboardRead", policy =>
        policy.RequireRole("SUPERADMIN", "ADMIN"));
    options.AddPolicy("StockSync", policy =>
        policy.RequireRole("SUPERADMIN", "ADMIN", "FACTURIER", "CAISSIER"));
    options.AddPolicy("FinanceSync", policy =>
        policy.RequireRole("SUPERADMIN", "ADMIN"));
});
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = JwtTokenService.BuildSigningKey(jwtOptions.SigningKey),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.AddCors(options =>
{
    var cors = builder.Configuration.GetSection("Cors").Get<ApiCorsOptions>() ?? new ApiCorsOptions();
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyHeader()
              .AllowAnyMethod();

        if (cors.AllowedOrigins.Length > 0)
        {
            policy.WithOrigins(cors.AllowedOrigins);
        }
    });
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
    try
    {
        var authService = scope.ServiceProvider.GetRequiredService<AuthService>();
        await authService.EnsureDevAdminAsync();
    }
    catch (Exception ex)
    {
        ApiStartupLogger.Write(app.Environment.ContentRootPath, "Startup failed: development admin initialization failed.", ex);
        throw;
    }
}

app.MapGet("/", () => Results.Ok(new { service = "CommercialMagDb.Api", status = "running" }));
app.MapGet("/health", async (DbConnectionFactory factory, IWebHostEnvironment env, CancellationToken ct) =>
{
    var databaseOk = false;
    try
    {
        await using var cn = factory.Create();
        await cn.OpenAsync(ct);
        await using var cmd = cn.CreateCommand();
        cmd.CommandText = "SELECT 1";
        await cmd.ExecuteScalarAsync(ct);
        databaseOk = true;
    }
    catch (Exception ex)
    {
        ApiStartupLogger.Write(env.ContentRootPath, "Health check database failure.", ex);
    }

    return Results.Ok(new
    {
        service = "CommercialMagDb.Api",
        status = "running",
        environment = env.EnvironmentName,
        database = databaseOk ? "OK" : "UNAVAILABLE",
        serverUtc = DateTime.UtcNow
    });
}).AllowAnonymous();

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

var stock = app.MapGroup("/api/stocksortie").RequireAuthorization("StockSync");
stock.MapPost("", async (StockSortieSyncRequest request, SyncService service) =>
{
    var result = await service.SyncStockSortieAsync(request);
    return Results.Ok(result);
});

var depenses = app.MapGroup("/api/depenses").RequireAuthorization("FinanceSync");
depenses.MapPost("", async (DepenseSyncRequest request, SyncService service) =>
{
    var result = await service.SyncDepenseAsync(request);
    return Results.Ok(result);
});

var dashboard = app.MapGroup("/api/dashboard").RequireAuthorization("DashboardRead");
dashboard.MapGet("/journalier", async (DateTime? date, DateTime? start, DateTime? end, DashboardService service) =>
{
    if (start.HasValue && end.HasValue)
    {
        var result = await service.GetJournalierAsync(start.Value, end.Value);
        return Results.Ok(result);
    }

    var resultByDate = await service.GetJournalierAsync(date ?? DateTime.Today);
    return Results.Ok(resultByDate);
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

var configuration = app.MapGroup("/api/configuration").RequireAuthorization("DashboardRead");
configuration.MapGet("/entreprise", async (DashboardService service, CancellationToken ct) =>
{
    var result = await service.GetEntrepriseConfigurationAsync(ct);
    return Results.Ok(result);
});

ApiStartupLogger.Write(app.Environment.ContentRootPath, $"Host configured env={app.Environment.EnvironmentName}");
try
{
    app.Run();
}
catch (Exception ex)
{
    ApiStartupLogger.Write(app.Environment.ContentRootPath, "Startup failed: host run failed.", ex);
    throw;
}
