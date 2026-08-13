namespace CommercialMagDb.Api.Infrastructure;

public sealed class JwtOptions
{
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string SigningKey { get; set; } = string.Empty;
    public int AccessTokenMinutes { get; set; } = 180;
    public int RefreshTokenDays { get; set; } = 7;
}

public sealed class DevSeedOptions
{
    public bool EnableAdminSeed { get; set; } = false;
    public string AdminUsername { get; set; } = "admin";
    public string AdminPassword { get; set; } = string.Empty;
    public string AdminRole { get; set; } = "ADMIN";
}

public sealed class ApiCorsOptions
{
    public string[] AllowedOrigins { get; set; } = [];
}
