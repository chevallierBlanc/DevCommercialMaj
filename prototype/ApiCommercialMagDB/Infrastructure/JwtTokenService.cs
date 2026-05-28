using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace CommercialMagDb.Api.Infrastructure;

public sealed class JwtTokenService(IConfiguration configuration)
{
    public static SymmetricSecurityKey BuildSigningKey(string signingKey) => new(Encoding.UTF8.GetBytes(signingKey));

    public (string Token, DateTime ExpiresAtUtc) CreateAccessToken(int userId, string username, string role)
    {
        var options = configuration.GetSection("Jwt").Get<JwtOptions>() ?? new JwtOptions();
        var now = DateTime.UtcNow;
        var expires = now.AddMinutes(options.AccessTokenMinutes);
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Name, username),
            new Claim(ClaimTypes.Role, role)
        };

        var credentials = new SigningCredentials(BuildSigningKey(options.SigningKey), SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: options.Issuer,
            audience: options.Audience,
            claims: claims,
            notBefore: now,
            expires: expires,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expires);
    }

    public (string Token, DateTime ExpiresAtUtc) CreateRefreshToken()
    {
        var options = configuration.GetSection("Jwt").Get<JwtOptions>() ?? new JwtOptions();
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(64));
        return (token, DateTime.UtcNow.AddDays(options.RefreshTokenDays));
    }
}
