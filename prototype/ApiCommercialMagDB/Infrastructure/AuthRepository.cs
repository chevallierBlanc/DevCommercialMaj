using System.Security.Cryptography;
using Microsoft.Data.SqlClient;

namespace CommercialMagDb.Api.Infrastructure;

public sealed class AuthRepository(DbConnectionFactory factory)
{
    public async Task<UserAuthRecord?> FindUserAsync(string username, CancellationToken ct = default)
    {
        await using var cn = factory.Create();
        await cn.OpenAsync(ct);
        const string sql = """
            SELECT TOP 1 UtilisateurId, NomUtilisateur, MotDePasseHash, MotDePasseSel, EstActif
            FROM Utilisateurs
            WHERE NomUtilisateur = @NomUtilisateur
            """;
        await using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@NomUtilisateur", username);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct)) return null;

        return new UserAuthRecord(
            reader.GetInt32(0),
            reader.GetString(1),
            (byte[])reader[2],
            (byte[])reader[3],
            reader.GetBoolean(4));
    }

    public async Task<string> GetRoleAsync(int userId, CancellationToken ct = default)
    {
        await using var cn = factory.Create();
        await cn.OpenAsync(ct);
        const string sql = """
            SELECT TOP 1 r.NomRole
            FROM UtilisateurRoles ur
            INNER JOIN Roles r ON r.RoleId = ur.RoleId
            WHERE ur.UtilisateurId = @UtilisateurId
            ORDER BY r.RoleId
            """;
        await using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@UtilisateurId", userId);
        var result = await cmd.ExecuteScalarAsync(ct);
        return result?.ToString() ?? string.Empty;
    }

    public async Task EnsureRoleAsync(string roleName, CancellationToken ct = default)
    {
        await using var cn = factory.Create();
        await cn.OpenAsync(ct);
        const string sql = """
            IF NOT EXISTS (SELECT 1 FROM Roles WHERE NomRole = @NomRole)
            BEGIN
                INSERT INTO Roles (NomRole) VALUES (@NomRole);
            END
            """;
        await using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@NomRole", roleName);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task EnsureDevAdminAsync(string username, string password, string roleName, PasswordHasher hasher, CancellationToken ct = default)
    {
        await EnsureRoleAsync(roleName, ct);
        await using var cn = factory.Create();
        await cn.OpenAsync(ct);

        var salt = hasher.GenerateSalt();
        var hash = hasher.HashPassword(password, salt);

        const string findRoleSql = "SELECT TOP 1 RoleId FROM Roles WHERE NomRole = @NomRole";
        int roleId;
        await using (var roleCmd = new SqlCommand(findRoleSql, cn))
        {
            roleCmd.Parameters.AddWithValue("@NomRole", roleName);
            roleId = Convert.ToInt32(await roleCmd.ExecuteScalarAsync(ct));
        }

        const string findUserSql = "SELECT TOP 1 UtilisateurId FROM Utilisateurs WHERE NomUtilisateur = @NomUtilisateur";
        int? userId = null;
        await using (var userCmd = new SqlCommand(findUserSql, cn))
        {
            userCmd.Parameters.AddWithValue("@NomUtilisateur", username);
            var existing = await userCmd.ExecuteScalarAsync(ct);
            if (existing is not null && existing != DBNull.Value)
            {
                userId = Convert.ToInt32(existing);
            }
        }

        if (userId is null)
        {
            const string insertUserSql = """
                INSERT INTO Utilisateurs (NomUtilisateur, MotDePasseHash, MotDePasseSel, EstActif)
                VALUES (@NomUtilisateur, @MotDePasseHash, @MotDePasseSel, 1);
                SELECT CAST(SCOPE_IDENTITY() AS INT);
                """;
            await using var insertUserCmd = new SqlCommand(insertUserSql, cn);
            insertUserCmd.Parameters.AddWithValue("@NomUtilisateur", username);
            insertUserCmd.Parameters.AddWithValue("@MotDePasseHash", hash);
            insertUserCmd.Parameters.AddWithValue("@MotDePasseSel", salt);
            userId = Convert.ToInt32(await insertUserCmd.ExecuteScalarAsync(ct));
        }
        else
        {
            const string updateSql = """
                UPDATE Utilisateurs
                SET MotDePasseHash = @MotDePasseHash,
                    MotDePasseSel = @MotDePasseSel,
                    EstActif = 1
                WHERE UtilisateurId = @UtilisateurId
                """;
            await using var updateCmd = new SqlCommand(updateSql, cn);
            updateCmd.Parameters.AddWithValue("@MotDePasseHash", hash);
            updateCmd.Parameters.AddWithValue("@MotDePasseSel", salt);
            updateCmd.Parameters.AddWithValue("@UtilisateurId", userId.Value);
            await updateCmd.ExecuteNonQueryAsync(ct);
        }

        const string roleAssignSql = """
            IF NOT EXISTS (SELECT 1 FROM UtilisateurRoles WHERE UtilisateurId = @UtilisateurId AND RoleId = @RoleId)
            BEGIN
                INSERT INTO UtilisateurRoles (UtilisateurId, RoleId) VALUES (@UtilisateurId, @RoleId);
            END
            """;
        await using (var roleAssignCmd = new SqlCommand(roleAssignSql, cn))
        {
            roleAssignCmd.Parameters.AddWithValue("@UtilisateurId", userId!.Value);
            roleAssignCmd.Parameters.AddWithValue("@RoleId", roleId);
            await roleAssignCmd.ExecuteNonQueryAsync(ct);
        }
    }
}

public sealed record UserAuthRecord(int UserId, string Username, byte[] PasswordHash, byte[] PasswordSalt, bool IsActive);
