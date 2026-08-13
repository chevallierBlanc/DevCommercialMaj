using CommercialMagDb.Api.Contracts.Auth;
using System.Security.Claims;

namespace CommercialMagDb.Api.Infrastructure;

public sealed class AuthService(
    AuthRepository authRepository,
    PasswordHasher passwordHasher,
    JwtTokenService tokenService,
    RefreshTokenStore refreshStore,
    IConfiguration configuration)
{
    public async Task<TokenResponse?> LoginAsync(string username, string password, CancellationToken ct = default)
    {
        var user = await authRepository.FindUserAsync(username, ct);
        if (user is null || !user.IsActive)
        {
            return null;
        }

        if (!passwordHasher.Verify(password, user.PasswordSalt, user.PasswordHash))
        {
            return null;
        }

        var role = await authRepository.GetRoleAsync(user.UserId, ct);
        var access = tokenService.CreateAccessToken(user.UserId, user.Username, role);
        var refresh = tokenService.CreateRefreshToken();
        refreshStore.Save(refresh.Token, new RefreshTokenEntry(user.UserId, user.Username, role, refresh.ExpiresAtUtc));
        return new TokenResponse
        {
            AccessToken = access.Token,
            AccessTokenExpiresAtUtc = access.ExpiresAtUtc,
            RefreshToken = refresh.Token,
            RefreshTokenExpiresAtUtc = refresh.ExpiresAtUtc,
            Username = user.Username,
            Role = role
        };
    }

    public async Task<TokenResponse?> RefreshAsync(string refreshToken, CancellationToken ct = default)
    {
        refreshStore.RemoveExpired();
        if (!refreshStore.TryGetValue(refreshToken, out var entry) || entry is null)
        {
            return null;
        }

        var access = tokenService.CreateAccessToken(entry.UserId, entry.Username, entry.Role);
        var nextRefresh = tokenService.CreateRefreshToken();
        refreshStore.Remove(refreshToken);
        refreshStore.Save(nextRefresh.Token, new RefreshTokenEntry(entry.UserId, entry.Username, entry.Role, nextRefresh.ExpiresAtUtc));

        return new TokenResponse
        {
            AccessToken = access.Token,
            AccessTokenExpiresAtUtc = access.ExpiresAtUtc,
            RefreshToken = nextRefresh.Token,
            RefreshTokenExpiresAtUtc = nextRefresh.ExpiresAtUtc,
            Username = entry.Username,
            Role = entry.Role
        };
    }

    public Task LogoutAsync(string refreshToken, CancellationToken ct = default)
    {
        refreshStore.Remove(refreshToken);
        return Task.CompletedTask;
    }

    public async Task EnsureDevAdminAsync(CancellationToken ct = default)
    {
        var seed = configuration.GetSection("DevSeed").Get<DevSeedOptions>() ?? new DevSeedOptions();
        if (!seed.EnableAdminSeed)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(seed.AdminUsername) ||
            string.IsNullOrWhiteSpace(seed.AdminPassword) ||
            seed.AdminPassword == "1234")
        {
            throw new InvalidOperationException("DevSeed est activé mais aucun mot de passe administrateur fort n'est configuré.");
        }

        await authRepository.EnsureDevAdminAsync(seed.AdminUsername, seed.AdminPassword, seed.AdminRole, passwordHasher, ct);
    }
}
