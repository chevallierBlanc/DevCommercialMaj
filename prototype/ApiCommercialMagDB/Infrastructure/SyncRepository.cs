using Microsoft.Data.SqlClient;
using CommercialMagDb.Api.Contracts.Sync;

namespace CommercialMagDb.Api.Infrastructure;

public sealed class SyncRepository(DbConnectionFactory factory)
{
    public async Task<SyncResult> SaveStockSortieAsync(StockSortieSyncRequest request, CancellationToken ct = default)
    {
        ValidateStockSortieRequest(request);

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

                var stockAvant = await ReadStockDisponibleAsync(cn, tx, line.ProduitId, ct);
                if (stockAvant < line.QuantiteBase)
                {
                    throw new InvalidOperationException($"Stock insuffisant pour le produit {line.ProduitId}. Disponible={stockAvant}; demandé={line.QuantiteBase}.");
                }

                var stockApres = stockAvant - line.QuantiteBase;
                await InsertStockSortieAsync(cn, tx, request, line, ct);
                await InsertMouvementAsync(cn, tx, request, line, stockAvant, stockApres, ct);
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

    private static void ValidateStockSortieRequest(StockSortieSyncRequest request)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.NumeroSortie))
        {
            throw new InvalidOperationException("Le numéro de sortie est obligatoire.");
        }

        foreach (var line in request.Lignes)
        {
            if (line.ProduitId <= 0)
            {
                throw new InvalidOperationException("Produit invalide dans la sortie stock.");
            }

            if (line.QuantiteSaisie <= 0m)
            {
                throw new InvalidOperationException($"Quantité commerciale invalide pour le produit {line.ProduitId}.");
            }

            if (line.QuantiteBase <= 0m)
            {
                throw new InvalidOperationException($"Quantité physique de référence invalide pour le produit {line.ProduitId}.");
            }
        }
    }

    private static async Task<decimal> ReadStockDisponibleAsync(SqlConnection cn, SqlTransaction tx, int produitId, CancellationToken ct)
    {
        const string sql = """
            SELECT ISNULL(e.TotalEntree, 0) - ISNULL(s.TotalSortie, 0) - ISNULL(p.TotalPerte, 0) AS StockDisponible
            FROM Produits pr WITH (UPDLOCK, HOLDLOCK)
            LEFT JOIN (
                SELECT ProduitId, SUM(ISNULL(QuantiteBase, 0)) AS TotalEntree
                FROM StockEntree WITH (UPDLOCK, HOLDLOCK)
                WHERE ProduitId = @ProduitId
                GROUP BY ProduitId
            ) e ON e.ProduitId = pr.ProduitId
            LEFT JOIN (
                SELECT ProduitId, SUM(ISNULL(QuantiteBase, 0)) AS TotalSortie
                FROM StockSortie WITH (UPDLOCK, HOLDLOCK)
                WHERE ProduitId = @ProduitId
                GROUP BY ProduitId
            ) s ON s.ProduitId = pr.ProduitId
            LEFT JOIN (
                SELECT ProduitId, SUM(ISNULL(QuantiteBase, 0)) AS TotalPerte
                FROM StockPerte WITH (UPDLOCK, HOLDLOCK)
                WHERE ProduitId = @ProduitId
                GROUP BY ProduitId
            ) p ON p.ProduitId = pr.ProduitId
            WHERE pr.ProduitId = @ProduitId
            """;

        await using var cmd = new SqlCommand(sql, cn, tx);
        cmd.Parameters.AddWithValue("@ProduitId", produitId);
        var result = await cmd.ExecuteScalarAsync(ct);
        if (result is null || result == DBNull.Value)
        {
            throw new InvalidOperationException($"Produit introuvable: {produitId}.");
        }

        return Convert.ToDecimal(result);
    }

    public async Task<SyncResult> SaveDepenseAsync(DepenseSyncRequest request, CancellationToken ct = default)
    {
        ValidateDepenseRequest(request);

        await using var cn = factory.Create();
        await cn.OpenAsync(ct);

        var dateDepense = (request.DateDepense ?? DateTime.UtcNow.Date).Date;
        var dateDepenseFin = dateDepense.AddDays(1);

        const string existsSql = """
            SELECT TOP 1 1
            FROM Depenses
            WHERE Categorie = @Categorie
              AND Montant = @Montant
              AND Devise = @Devise
              AND ISNULL(Description,'') = ISNULL(@Description,'')
              AND DateDepense >= @DateDepense
              AND DateDepense < @DateDepenseFin
              AND Source = @Source
              AND TypeDepense = @TypeDepense
            """;
        await using var existsCmd = new SqlCommand(existsSql, cn);
        AddDepenseParameters(existsCmd, request, dateDepense);
        existsCmd.Parameters.AddWithValue("@DateDepenseFin", dateDepenseFin);
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
        AddDepenseParameters(insertCmd, request, dateDepense);
        await insertCmd.ExecuteNonQueryAsync(ct);
        return new SyncResult(1, 0, null);
    }

    private static void ValidateDepenseRequest(DepenseSyncRequest request)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.Categorie))
        {
            throw new InvalidOperationException("La catégorie de dépense est obligatoire.");
        }

        if (request.Montant <= 0m)
        {
            throw new InvalidOperationException("Le montant de dépense doit être supérieur à zéro.");
        }

        if (string.IsNullOrWhiteSpace(request.Devise))
        {
            throw new InvalidOperationException("La devise de dépense est obligatoire.");
        }

        if (string.IsNullOrWhiteSpace(request.Source))
        {
            throw new InvalidOperationException("La source de dépense est obligatoire.");
        }

        if (string.IsNullOrWhiteSpace(request.TypeDepense))
        {
            throw new InvalidOperationException("Le type de dépense est obligatoire.");
        }
    }

    private static void AddDepenseParameters(SqlCommand cmd, DepenseSyncRequest request, DateTime dateDepense)
    {
        cmd.Parameters.AddWithValue("@Categorie", request.Categorie.Trim());
        cmd.Parameters.AddWithValue("@Montant", request.Montant);
        cmd.Parameters.AddWithValue("@Devise", request.Devise.Trim());
        cmd.Parameters.AddWithValue("@Description", DbNullIfNull(request.Description));
        cmd.Parameters.AddWithValue("@DateDepense", dateDepense);
        cmd.Parameters.AddWithValue("@Source", request.Source.Trim());
        cmd.Parameters.AddWithValue("@TypeDepense", request.TypeDepense.Trim());
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

    private static async Task InsertMouvementAsync(SqlConnection cn, SqlTransaction tx, StockSortieSyncRequest request, StockSortieSyncLine line, decimal stockAvant, decimal stockApres, CancellationToken ct)
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
                    (@ProduitId, 'SORTIE_MANUELLE', @Quantite, @QuantiteBase, @Unite, @StockAvant, @StockApres, @Reference, @Observation, NULL, @EffectuePar, @NumeroMouvement)
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
        cmd.Parameters.AddWithValue("@StockAvant", stockAvant);
        cmd.Parameters.AddWithValue("@StockApres", stockApres);
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
