using CommercialMagDb.Api.Contracts.Dashboard;
using System.Data;
using Microsoft.Data.SqlClient;

namespace CommercialMagDb.Api.Infrastructure;

public sealed class DashboardRepository(DbConnectionFactory factory)
{
    public async Task<JournalierDashboardResponse> GetJournalierAsync(DateTime date, CancellationToken ct = default)
    {
        await using var cn = factory.Create();
        await cn.OpenAsync(ct);

        var response = new JournalierDashboardResponse
        {
            Date = date.Date,
            CaDuJour = await ScalarDecimalAsync(cn, """
                SELECT ISNULL(SUM(CASE WHEN UPPER(ISNULL(ss.StatutPaiement,'')) <> 'GRATUIT' THEN ISNULL(ss.MontantLigne,0) ELSE 0 END),0)
                FROM StockSortie ss
                WHERE CONVERT(date, ss.DateSortie) = @DateRef
                """, ct, ("@DateRef", date.Date)),
            TotalSorties = await ScalarDecimalAsync(cn, """
                SELECT ISNULL(SUM(ISNULL(ss.QuantiteBase,0)),0)
                FROM StockSortie ss
                WHERE CONVERT(date, ss.DateSortie) = @DateRef
                  AND UPPER(ISNULL(ss.Source,'')) = 'SORTIE_MANUELLE'
                """, ct, ("@DateRef", date.Date)),
            DepensesDuJour = await ScalarDecimalAsync(cn, """
                SELECT ISNULL(SUM(ISNULL(d.Montant,0)),0)
                FROM Depenses d
                WHERE CONVERT(date, d.DateDepense) = @DateRef
                """, ct, ("@DateRef", date.Date))
        };
        response.BeneficeEstime = response.CaDuJour - response.DepensesDuJour;

        response.ProduitsVendus = await QueryDailyProductsAsync(cn, date, ct);
        response.SortiesManuelles = await QueryManualExitsAsync(cn, date, ct);
        response.DepensesParCategorie = await QueryExpensesByCategoryAsync(cn, date, ct);
        response.AlertesStockFaible = await QueryStockAlertsAsync(cn, ct);
        response.SeriesVentes = await QuerySeriesAsync(cn, """
            SELECT CONVERT(date, ss.DateSortie) AS Label, ISNULL(SUM(ISNULL(ss.MontantLigne,0)),0) AS Value
            FROM StockSortie ss
            WHERE CONVERT(date, ss.DateSortie) = @DateRef
            GROUP BY CONVERT(date, ss.DateSortie)
            ORDER BY CONVERT(date, ss.DateSortie)
            """, ct, ("@DateRef", date.Date));
        response.SeriesDepenses = await QuerySeriesAsync(cn, """
            SELECT CONVERT(date, d.DateDepense) AS Label, ISNULL(SUM(ISNULL(d.Montant,0)),0) AS Value
            FROM Depenses d
            WHERE CONVERT(date, d.DateDepense) = @DateRef
            GROUP BY CONVERT(date, d.DateDepense)
            ORDER BY CONVERT(date, d.DateDepense)
            """, ct, ("@DateRef", date.Date));
        return response;
    }

    public async Task<MensuelDashboardResponse> GetMensuelAsync(int year, int month, CancellationToken ct = default)
    {
        await using var cn = factory.Create();
        await cn.OpenAsync(ct);
        var start = new DateTime(year, month, 1);
        var end = start.AddMonths(1);

        var response = new MensuelDashboardResponse
        {
            Year = year,
            Month = month,
            CaMensuel = await ScalarDecimalAsync(cn, """
                SELECT ISNULL(SUM(CASE WHEN UPPER(ISNULL(ss.StatutPaiement,'')) <> 'GRATUIT' THEN ISNULL(ss.MontantLigne,0) ELSE 0 END),0)
                FROM StockSortie ss
                WHERE ss.DateSortie >= @StartDate AND ss.DateSortie < @EndDate
                """, ct, ("@StartDate", start), ("@EndDate", end)),
            DepensesMensuelles = await ScalarDecimalAsync(cn, """
                SELECT ISNULL(SUM(ISNULL(d.Montant,0)),0)
                FROM Depenses d
                WHERE d.DateDepense >= @StartDate AND d.DateDepense < @EndDate
                """, ct, ("@StartDate", start), ("@EndDate", end))
        };
        response.BeneficeEstime = response.CaMensuel - response.DepensesMensuelles;
        response.TopProduits = await QueryMonthlyTopProductsAsync(cn, start, end, ct);
        response.TopDepenses = await QueryExpensesByCategoryAsync(cn, start, ct, end);
        response.EvolutionVentes = await QuerySeriesAsync(cn, """
            SELECT CONVERT(date, ss.DateSortie) AS Label, ISNULL(SUM(ISNULL(ss.MontantLigne,0)),0) AS Value
            FROM StockSortie ss
            WHERE ss.DateSortie >= @StartDate AND ss.DateSortie < @EndDate
            GROUP BY CONVERT(date, ss.DateSortie)
            ORDER BY CONVERT(date, ss.DateSortie)
            """, ct, ("@StartDate", start), ("@EndDate", end));
        response.EvolutionSorties = await QuerySeriesAsync(cn, """
            SELECT CONVERT(date, ss.DateSortie) AS Label, ISNULL(SUM(ISNULL(ss.QuantiteBase,0)),0) AS Value
            FROM StockSortie ss
            WHERE ss.DateSortie >= @StartDate AND ss.DateSortie < @EndDate
              AND UPPER(ISNULL(ss.Source,'')) = 'SORTIE_MANUELLE'
            GROUP BY CONVERT(date, ss.DateSortie)
            ORDER BY CONVERT(date, ss.DateSortie)
            """, ct, ("@StartDate", start), ("@EndDate", end));
        response.EvolutionDepenses = await QuerySeriesAsync(cn, """
            SELECT CONVERT(date, d.DateDepense) AS Label, ISNULL(SUM(ISNULL(d.Montant,0)),0) AS Value
            FROM Depenses d
            WHERE d.DateDepense >= @StartDate AND d.DateDepense < @EndDate
            GROUP BY CONVERT(date, d.DateDepense)
            ORDER BY CONVERT(date, d.DateDepense)
            """, ct, ("@StartDate", start), ("@EndDate", end));
        return response;
    }

    public async Task<AnnuelDashboardResponse> GetAnnuelAsync(int year, CancellationToken ct = default)
    {
        await using var cn = factory.Create();
        await cn.OpenAsync(ct);
        var start = new DateTime(year, 1, 1);
        var end = start.AddYears(1);

        var response = new AnnuelDashboardResponse
        {
            Year = year,
            CaAnnuel = await ScalarDecimalAsync(cn, """
                SELECT ISNULL(SUM(CASE WHEN UPPER(ISNULL(ss.StatutPaiement,'')) <> 'GRATUIT' THEN ISNULL(ss.MontantLigne,0) ELSE 0 END),0)
                FROM StockSortie ss
                WHERE ss.DateSortie >= @StartDate AND ss.DateSortie < @EndDate
                """, ct, ("@StartDate", start), ("@EndDate", end)),
            DepensesAnnuelles = await ScalarDecimalAsync(cn, """
                SELECT ISNULL(SUM(ISNULL(d.Montant,0)),0)
                FROM Depenses d
                WHERE d.DateDepense >= @StartDate AND d.DateDepense < @EndDate
                """, ct, ("@StartDate", start), ("@EndDate", end))
        };
        response.BeneficeEstime = response.CaAnnuel - response.DepensesAnnuelles;
        response.VentesParMois = await QueryMonthSeriesAsync(cn, """
            SELECT RIGHT('0' + CAST(MONTH(ss.DateSortie) AS varchar(2)), 2) AS Label, ISNULL(SUM(ISNULL(ss.MontantLigne,0)),0) AS Value
            FROM StockSortie ss
            WHERE ss.DateSortie >= @StartDate AND ss.DateSortie < @EndDate
            GROUP BY MONTH(ss.DateSortie)
            ORDER BY MONTH(ss.DateSortie)
            """, ct, ("@StartDate", start), ("@EndDate", end));
        response.DepensesParMois = await QueryMonthSeriesAsync(cn, """
            SELECT RIGHT('0' + CAST(MONTH(d.DateDepense) AS varchar(2)), 2) AS Label, ISNULL(SUM(ISNULL(d.Montant,0)),0) AS Value
            FROM Depenses d
            WHERE d.DateDepense >= @StartDate AND d.DateDepense < @EndDate
            GROUP BY MONTH(d.DateDepense)
            ORDER BY MONTH(d.DateDepense)
            """, ct, ("@StartDate", start), ("@EndDate", end));
        response.BeneficesParMois = response.VentesParMois.Zip(response.DepensesParMois, (v, d) => new PeriodSeriesPoint { Label = v.Label, Value = v.Value - d.Value }).ToList();
        response.CategoriesDepensesGourmandes = await QueryExpensesByCategoryAsync(cn, start, ct, end);
        response.TopProduits = await QueryMonthlyTopProductsAsync(cn, start, end, ct);
        return response;
    }

    private static async Task<decimal> ScalarDecimalAsync(SqlConnection cn, string sql, CancellationToken ct, params (string Name, object? Value)[] parameters)
    {
        await using var cmd = new SqlCommand(sql, cn);
        foreach (var (name, value) in parameters)
        {
            cmd.Parameters.AddWithValue(name, value ?? DBNull.Value);
        }
        var result = await cmd.ExecuteScalarAsync(ct);
        return result is null || result == DBNull.Value ? 0m : Convert.ToDecimal(result);
    }

    private static async Task<List<PeriodSeriesPoint>> QuerySeriesAsync(SqlConnection cn, string sql, CancellationToken ct, params (string Name, object? Value)[] parameters)
    {
        await using var cmd = new SqlCommand(sql, cn);
        foreach (var (name, value) in parameters)
        {
            cmd.Parameters.AddWithValue(name, value ?? DBNull.Value);
        }
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        var list = new List<PeriodSeriesPoint>();
        while (await reader.ReadAsync(ct))
        {
            list.Add(new PeriodSeriesPoint { Label = reader.GetValue(0).ToString() ?? string.Empty, Value = reader.IsDBNull(1) ? 0m : Convert.ToDecimal(reader.GetValue(1)) });
        }
        return list;
    }

    private static async Task<List<PeriodSeriesPoint>> QueryMonthSeriesAsync(SqlConnection cn, string sql, CancellationToken ct, params (string Name, object? Value)[] parameters)
    {
        var list = await QuerySeriesAsync(cn, sql, ct, parameters);
        return list;
    }

    private static async Task<List<ExpenseRow>> QueryExpensesByCategoryAsync(SqlConnection cn, DateTime start, CancellationToken ct, DateTime? end = null)
    {
        var sql = end is null
            ? """
              SELECT ISNULL(Categorie, 'Sans catégorie') AS Category, ISNULL(SUM(ISNULL(Montant,0)),0) AS Amount
              FROM Depenses
              WHERE CONVERT(date, DateDepense) = @DateRef
              GROUP BY Categorie
              ORDER BY Amount DESC
              """
            : """
              SELECT ISNULL(Categorie, 'Sans catégorie') AS Category, ISNULL(SUM(ISNULL(Montant,0)),0) AS Amount
              FROM Depenses
              WHERE DateDepense >= @StartDate AND DateDepense < @EndDate
              GROUP BY Categorie
              ORDER BY Amount DESC
              """;
        await using var cmd = new SqlCommand(sql, cn);
        if (end is null)
        {
            cmd.Parameters.AddWithValue("@DateRef", start.Date);
        }
        else
        {
            cmd.Parameters.AddWithValue("@StartDate", start);
            cmd.Parameters.AddWithValue("@EndDate", end.Value);
        }
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        var list = new List<ExpenseRow>();
        while (await reader.ReadAsync(ct))
        {
            list.Add(new ExpenseRow
            {
                Category = reader.GetValue(0).ToString() ?? string.Empty,
                Amount = reader.IsDBNull(1) ? 0m : Convert.ToDecimal(reader.GetValue(1))
            });
        }
        return list;
    }

    private static async Task<List<DailyProductRow>> QueryDailyProductsAsync(SqlConnection cn, DateTime date, CancellationToken ct)
    {
        const string sql = """
            SELECT TOP 10 p.Libelle,
                   ISNULL(SUM(ss.QuantiteBase),0) AS Quantite,
                   ISNULL(MAX(ss.TypeVente), '') AS TypeVente,
                   ISNULL(SUM(ss.MontantLigne),0) AS Montant,
                   ISNULL(MIN(ss.DateSortie), GETDATE()) AS Heure,
                   ISNULL(MAX(u.NomUtilisateur), '') AS Agent
            FROM StockSortie ss
            INNER JOIN Produits p ON p.ProduitId = ss.ProduitId
            LEFT JOIN Utilisateurs u ON u.UtilisateurId = ss.CreePar
            WHERE CONVERT(date, ss.DateSortie) = @DateRef
              AND UPPER(ISNULL(ss.StatutPaiement,'')) <> 'GRATUIT'
            GROUP BY p.Libelle
            ORDER BY SUM(ss.MontantLigne) DESC
            """;
        await using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@DateRef", date.Date);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        var list = new List<DailyProductRow>();
        while (await reader.ReadAsync(ct))
        {
            list.Add(new DailyProductRow
            {
                Product = reader.GetString(0),
                QuantitySold = reader.IsDBNull(1) ? 0m : Convert.ToDecimal(reader.GetValue(1)),
                TypeVente = reader.GetValue(2).ToString() ?? string.Empty,
                AmountGenerated = reader.IsDBNull(3) ? 0m : Convert.ToDecimal(reader.GetValue(3)),
                Hour = reader.IsDBNull(4) ? date : Convert.ToDateTime(reader.GetValue(4)),
                Agent = reader.GetValue(5).ToString() ?? string.Empty
            });
        }
        return list;
    }

    private static async Task<List<ManualExitRow>> QueryManualExitsAsync(SqlConnection cn, DateTime date, CancellationToken ct)
    {
        const string sql = """
            SELECT TOP 50 p.Libelle,
                   ISNULL(ss.QuantiteBase,0) AS Quantite,
                   ISNULL(m.Libelle, ss.Source) AS Motif,
                   ISNULL(m.Nature, '') AS Category,
                   ISNULL(u.NomUtilisateur, '') AS Utilisateur,
                   ss.DateSortie,
                   ISNULL(ss.MontantLigne,0) AS Montant,
                   ISNULL(ss.Observation, '') AS Observation
            FROM StockSortie ss
            INNER JOIN Produits p ON p.ProduitId = ss.ProduitId
            LEFT JOIN MotifSortie m ON m.MotifId = ss.MotifId
            LEFT JOIN Utilisateurs u ON u.UtilisateurId = ss.CreePar
            WHERE CONVERT(date, ss.DateSortie) = @DateRef
              AND UPPER(ISNULL(ss.Source,'')) = 'SORTIE_MANUELLE'
            ORDER BY ss.DateSortie DESC
            """;
        await using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@DateRef", date.Date);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        var list = new List<ManualExitRow>();
        while (await reader.ReadAsync(ct))
        {
            list.Add(new ManualExitRow
            {
                Product = reader.GetString(0),
                Quantity = reader.IsDBNull(1) ? 0m : Convert.ToDecimal(reader.GetValue(1)),
                Motif = reader.GetValue(2).ToString() ?? string.Empty,
                Category = reader.GetValue(3).ToString() ?? string.Empty,
                User = reader.GetValue(4).ToString() ?? string.Empty,
                Date = reader.IsDBNull(5) ? date : Convert.ToDateTime(reader.GetValue(5)),
                Amount = reader.IsDBNull(6) ? 0m : Convert.ToDecimal(reader.GetValue(6)),
                Observation = reader.GetValue(7).ToString() ?? string.Empty
            });
        }
        return list;
    }

    private static async Task<List<StockAlertRow>> QueryStockAlertsAsync(SqlConnection cn, CancellationToken ct)
    {
        const string sql = """
            SELECT TOP 10 p.Libelle, ISNULL(s.QuantiteStock,0) AS Stock
            FROM Produits p
            INNER JOIN vStockProduit s ON s.ProduitId = p.ProduitId
            WHERE s.QuantiteStock <= 20
            ORDER BY s.QuantiteStock ASC, p.Libelle ASC
            """;
        await using var cmd = new SqlCommand(sql, cn);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        var list = new List<StockAlertRow>();
        while (await reader.ReadAsync(ct))
        {
            list.Add(new StockAlertRow
            {
                Product = reader.GetValue(0).ToString() ?? string.Empty,
                Stock = reader.IsDBNull(1) ? 0m : Convert.ToDecimal(reader.GetValue(1))
            });
        }
        return list;
    }

    private static async Task<List<DailyProductRow>> QueryMonthlyTopProductsAsync(SqlConnection cn, DateTime start, DateTime end, CancellationToken ct)
    {
        const string sql = """
            SELECT TOP 10 p.Libelle,
                   ISNULL(SUM(ss.QuantiteBase),0) AS Quantite,
                   ISNULL(MAX(ss.TypeVente), '') AS TypeVente,
                   ISNULL(SUM(ss.MontantLigne),0) AS Montant,
                   ISNULL(MIN(ss.DateSortie), GETDATE()) AS Heure,
                   ISNULL(MAX(u.NomUtilisateur), '') AS Agent
            FROM StockSortie ss
            INNER JOIN Produits p ON p.ProduitId = ss.ProduitId
            LEFT JOIN Utilisateurs u ON u.UtilisateurId = ss.CreePar
            WHERE ss.DateSortie >= @StartDate AND ss.DateSortie < @EndDate
              AND UPPER(ISNULL(ss.StatutPaiement,'')) <> 'GRATUIT'
            GROUP BY p.Libelle
            ORDER BY SUM(ss.MontantLigne) DESC
            """;
        await using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@StartDate", start);
        cmd.Parameters.AddWithValue("@EndDate", end);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        var list = new List<DailyProductRow>();
        while (await reader.ReadAsync(ct))
        {
            list.Add(new DailyProductRow
            {
                Product = reader.GetValue(0).ToString() ?? string.Empty,
                QuantitySold = reader.IsDBNull(1) ? 0m : Convert.ToDecimal(reader.GetValue(1)),
                TypeVente = reader.GetValue(2).ToString() ?? string.Empty,
                AmountGenerated = reader.IsDBNull(3) ? 0m : Convert.ToDecimal(reader.GetValue(3)),
                Hour = reader.IsDBNull(4) ? start : Convert.ToDateTime(reader.GetValue(4)),
                Agent = reader.GetValue(5).ToString() ?? string.Empty
            });
        }
        return list;
    }
}
