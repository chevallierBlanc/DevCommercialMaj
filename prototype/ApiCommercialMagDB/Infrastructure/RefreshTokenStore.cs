using System.Collections.Concurrent;

namespace CommercialMagDb.Api.Infrastructure;

public sealed class RefreshTokenStore
{
    private readonly ConcurrentDictionary<string, RefreshTokenEntry> _tokens = new(StringComparer.Ordinal);

    public void Save(string token, RefreshTokenEntry entry) => _tokens[token] = entry;
    public bool TryGetValue(string token, out RefreshTokenEntry? entry) => _tokens.TryGetValue(token, out entry);
    public void Remove(string token) => _tokens.TryRemove(token, out _);
    public void RemoveExpired()
    {
        var now = DateTime.UtcNow;
        foreach (var kv in _tokens)
        {
            if (kv.Value.ExpiresAtUtc <= now)
            {
                _tokens.TryRemove(kv.Key, out _);
            }
        }
    }
}

public sealed record RefreshTokenEntry(int UserId, string Username, string Role, DateTime ExpiresAtUtc);
