using System.Security.Cryptography;
using System.Text;

namespace CommercialMagDb.Api.Infrastructure;

public sealed class PasswordHasher
{
    public byte[] GenerateSalt()
    {
        var salt = new byte[16];
        RandomNumberGenerator.Fill(salt);
        return salt;
    }

    public byte[] HashPassword(string password, byte[] salt)
    {
        using var derive = new Rfc2898DeriveBytes(password, salt, 10000, HashAlgorithmName.SHA1);
        return derive.GetBytes(32);
    }

    public bool Verify(string password, byte[] salt, byte[] expectedHash)
    {
        var hash = HashPassword(password, salt);
        return CryptographicOperations.FixedTimeEquals(hash, expectedHash);
    }
}
