using Microsoft.Data.SqlClient;
using CommercialMagDb.Api.Contracts.Sync;

namespace CommercialMagDb.Api.Infrastructure;

public sealed class SyncRepository(DbConnectionFactory factory)
{
    public async Task<SyncResult> SaveStockSortieAsync(StockSortieSyncRequest request, CancellationToken ct = default)
    {
        if (request.Lignes.Count == 0)
        {
            return new SyncResult(0, 0, "Aucune ligne fournie.");
        }

        var inserted = 0;
        var skipped = 0;
        await using var cn = factory.Create();
        await cn.OpenAsync(ct);
        using var tx = cn.BeginTransaction();

        try
        {
            foreach (var line in request.Lignes)
            {
                if (await ExistsStockSortieAsync(cn, tx, request.NumeroSortie, line, request, ct))
                {
                    skipped++;
                    continue;
                }

                await InsertStockSortieAsync(cn, tx, request, line, ct);
                await InsertMouvementAsync(cn, tx, request, line, ct);
                inserted++;
            }

            await tx.CommitAsync(ct);
            return new SyncResult(inserted, skipped, null);
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(ct);
            throw new InvalidOperationException("Echec de synchronisation StockSortie: " + ex.Message, ex);
        }
    }

    public async Task<SyncResult> SaveDepenseAsync(DepenseSyncRequest request, CancellationToken ct = default)
    {
        await using var cn = factory.Create();
        await cn.OpenAsync(ct);

        const string existsSql = """
            SELECT TOP 1 1
            FROM Depenses
            WHERE Categorie = @Categorie
              AND Montant = @Montant
              AND Devise = @Devise
              AND ISNULL(Description,'') = ISNULL(@Description,'')
              AND CONVERT(date, DateDepense) = CONVERT(date, @DateDepense)
              AND Source = @Source
              AND TypeDepense = @TypeDepense
            """;
        await using var existsCmd = new SqlCommand(existsSql, cn);
        AddDepenseParameters(existsCmd, request);
        var exists = await existsCmd.ExecuteScalarAsync(ct);
        if (exists is not null)
        {
            return new SyncResult(0, 1, null);
        }

        const string insertSql = """
            INSERT INTO Depenses (Categorie, Montant, Devise, Description, DateDepense, Source, TypeDepense, CreePar)
            VALUES (@Categorie, @Montant, @Devise, @Description, @DateDepense, @Source, @TypeDepense, @CreePar)
            """;
        await using var insertCmd = new SqlCommand(insertSql, cn);
        AddDepenseParameters(insertCmd, request);
        await insertCmd.ExecuteNonQueryAsync(ct);
        return new SyncResult(1, 0, null);
    }

    private static void AddDepenseParameters(SqlCommand cmd, DepenseSyncRequest request)
    {
        cmd.Parameters.AddWithValue("@Categorie", request.Categorie);
        cmd.Parameters.AddWithValue("@Montant", request.Montant);
        cmd.Parameters.AddWithValue("@Devise", request.Devise);
        cmd.Parameters.AddWithValue("@Description", DbNullIfNull(request.Description));
        cmd.Parameters.AddWithValue("@DateDepense", request.DateDepense ?? DateTime.UtcNow.Date);
        cmd.Parameters.AddWithValue("@Source", request.Source);
        cmd.Parameters.AddWithValue("@TypeDepense", request.TypeDepense);
        cmd.Parameters.AddWithValue("@CreePar", DbNullIfNull(request.CreePar));
    }

    private static async Task<bool> ExistsStockSortieAsync(SqlConnection cn, SqlTransaction tx, string numeroSortie, StockSortieSyncLine line, StockSortieSyncRequest request, CancellationToken ct)
    {
        const string sql = """
            SELECT TOP 1 1
            FROM StockSortie
            WHERE NumeroSortie = @NumeroSortie
              AND ProduitId = @ProduitId
              AND QuantiteBase = @QuantiteBase
              AND ISNULL(Source,'') = ISNULL(@Source,'')
            """;
        await using var cmd = new SqlCommand(sql, cn, tx);
        cmd.Parameters.AddWithValue("@NumeroSortie", numeroSortie);
        cmd.Parameters.AddWithValue("@ProduitId", line.ProduitId);
        cmd.Parameters.AddWithValue("@QuantiteBase", line.QuantiteBase);
        cmd.Parameters.AddWithValue("@Source", request.Source ?? "SORTIE_MANUELLE");
        var exists = await cmd.ExecuteScalarAsync(ct);
        return exists is not null;
    }

    private static async Task InsertStockSortieAsync(SqlConnection cn, SqlTransaction tx, StockSortieSyncRequest request, StockSortieSyncLine line, CancellationToken ct)
    {
        const string sql = """
            INSERT INTO StockSortie
                (ProduitId, QuantiteSaisie, Unite, QuantiteBase, DateSortie, Source, RefSource, CreePar, NumeroSortie, ClientId, MotifId, TypeVente, PrixUnitaire, MontantLigne, StatutPaiement, MontantPaye, ResteAPayer, Observation)
            VALUES
                (@ProduitId, @QuantiteSaisie, @Unite, @QuantiteBase, @DateSortie, @Source, @RefSource, @CreePar, @NumeroSortie, @ClientId, @MotifId, @TypeVente, @PrixUnitaire, @MontantLigne, @StatutPaiement, @MontantPaye, @ResteAPayer, @Observation)
            """;
        await using var cmd = new SqlCommand(sql, cn, tx);
        cmd.Parameters.AddWithValue("@ProduitId", line.ProduitId);
        cmd.Parameters.AddWithValue("@QuantiteSaisie", line.QuantiteSaisie);
        cmd.Parameters.AddWithValue("@Unite", DbNullIfNull(line.Unite));
        cmd.Parameters.AddWithValue("@QuantiteBase", line.QuantiteBase);
        cmd.Parameters.AddWithValue("@DateSortie", request.DateSortie ?? DateTime.UtcNow);
        cmd.Parameters.AddWithValue("@Source", request.Source ?? "SORTIE_MANUELLE");
        cmd.Parameters.AddWithValue("@RefSource", request.NumeroSortie);
        cmd.Parameters.AddWithValue("@CreePar", DbNullIfNull(request.CreePar));
        cmd.Parameters.AddWithValue("@NumeroSortie", request.NumeroSortie);
        cmd.Parameters.AddWithValue("@ClientId", DbNullIfNull(request.ClientId));
        cmd.Parameters.AddWithValue("@MotifId", DbNullIfNull(request.MotifId));
        cmd.Parameters.AddWithValue("@TypeVente", DbNullIfNull(line.TypeVente));
        cmd.Parameters.AddWithValue("@PrixUnitaire", DbNullIfNull(line.PrixUnitaire));
        cmd.Parameters.AddWithValue("@MontantLigne", DbNullIfNull(line.MontantLigne));
        cmd.Parameters.AddWithValue("@StatutPaiement", DbNullIfNull(line.StatutPaiement));
        cmd.Parameters.AddWithValue("@MontantPaye", DbNullIfNull(line.MontantPaye));
        cmd.Parameters.AddWithValue("@ResteAPayer", DbNullIfNull(line.ResteAPayer));
        cmd.Parameters.AddWithValue("@Observation", DbNullIfNull(string.IsNullOrWhiteSpace(line.Observation) ? request.Observation : line.Observation));
        await cmd.ExecuteNonQueryAsync(ct);
    }

    private static async Task InsertMouvementAsync(SqlConnection cn, SqlTransaction tx, StockSortieSyncRequest request, StockSortieSyncLine line, CancellationToken ct)
    {
        const string sql = """
            IF NOT EXISTS (
                SELECT 1 FROM MouvementsStock
                WHERE ProduitId = @ProduitId
                  AND TypeMouvement = 'SORTIE_MANUELLE'
                  AND Reference = @Reference
                  AND QuantiteBase = @QuantiteBase
            )
            BEGIN
                INSERT INTO MouvementsStock
                    (ProduitId, TypeMouvement, Quantite, QuantiteBase, Unite, StockAvant, StockApres, Reference, Observation, TypePerte, EffectuePar, NumeroMouvement)
                VALUES
                    (@ProduitId, 'SORTIE_MANUELLE', @Quantite, @QuantiteBase, @Unite, 0, 0, @Reference, @Observation, NULL, @EffectuePar, @NumeroMouvement)
            END
            """;
        await using var cmd = new SqlCommand(sql, cn, tx);
        var numeroMouvement = $"SYNC-{request.NumeroSortie}-{line.ProduitId}";
        if (numeroMouvement.Length > 30)
        {
            numeroMouvement = numeroMouvement[..30];
        }
        cmd.Parameters.AddWithValue("@ProduitId", line.ProduitId);
        cmd.Parameters.AddWithValue("@Quantite", line.QuantiteSaisie);
        cmd.Parameters.AddWithValue("@QuantiteBase", line.QuantiteBase);
        cmd.Parameters.AddWithValue("@Unite", DbNullIfNull(line.Unite));
        cmd.Parameters.AddWithValue("@Reference", request.NumeroSortie);
        cmd.Parameters.AddWithValue("@Observation", DbNullIfNull(string.IsNullOrWhiteSpace(line.Observation) ? request.Observation : line.Observation));
        cmd.Parameters.AddWithValue("@EffectuePar", DbNullIfNull(request.CreePar));
        cmd.Parameters.AddWithValue("@NumeroMouvement", numeroMouvement);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    private static object DbNullIfNull<T>(T? value) where T : struct
    {
        return value.HasValue ? value.Value : DBNull.Value;
    }

    private static object DbNullIfNull(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? DBNull.Value : value!;
    }
}

public sealed record SyncResult(int Inserted, int Skipped, string? Message);
