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
        var start = date.Date;
        var end = start.AddDays(1);
        var totals = await LoadTotalsAsync(cn, start, end, ct);

        var response = new JournalierDashboardResponse
        {
            Date = date.Date,
            CaDuJour = totals.MontantTotalGenere,
            TotalSorties = totals.TotalVentes + totals.TotalSortiesManuelles + totals.TotalPertes,
            DepensesDuJour = await ScalarDecimalAsync(cn, """
                SELECT ISNULL(SUM(ISNULL(d.Montant,0)),0)
                FROM Depenses d
                WHERE CONVERT(date, d.DateDepense) = @DateRef
                """, ct, ("@DateRef", date.Date))
        };
        response.BeneficeEstime = response.CaDuJour - response.DepensesDuJour;
        ApplyTotals(response, totals);

        response.ProduitsVendus = await QueryDailyProductsAsync(cn, date, ct);
        response.SortiesManuelles = await QueryManualExitsAsync(cn, date, ct);
        response.DepensesParCategorie = await QueryExpensesByCategoryAsync(cn, date, ct);
        response.AlertesStockFaible = await QueryStockAlertsAsync(cn, ct);
        response.SeriesVentes = await QuerySeriesAsync(cn, """
            SELECT CONVERT(date, ss.DateSortie) AS Label, ISNULL(SUM(ISNULL(ss.MontantLigne,0)),0) AS Value
            FROM StockSortie ss
            WHERE CONVERT(date, ss.DateSortie) = @DateRef
              AND UPPER(ISNULL(ss.Source,'')) = 'VENTE'
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
        var totals = await LoadTotalsAsync(cn, start, end, ct);

        var response = new MensuelDashboardResponse
        {
            Year = year,
            Month = month,
            CaMensuel = totals.MontantTotalGenere,
            DepensesMensuelles = await ScalarDecimalAsync(cn, """
                SELECT ISNULL(SUM(ISNULL(d.Montant,0)),0)
                FROM Depenses d
                WHERE d.DateDepense >= @StartDate AND d.DateDepense < @EndDate
                """, ct, ("@StartDate", start), ("@EndDate", end))
        };
        response.BeneficeEstime = response.CaMensuel - response.DepensesMensuelles;
        ApplyTotals(response, totals);
        response.TopProduits = await QueryMonthlyTopProductsAsync(cn, start, end, ct);
        response.TopDepenses = await QueryExpensesByCategoryAsync(cn, start, ct, end);
        response.EvolutionVentes = await QuerySeriesAsync(cn, """
            SELECT CONVERT(date, ss.DateSortie) AS Label, ISNULL(SUM(ISNULL(ss.MontantLigne,0)),0) AS Value
            FROM StockSortie ss
            WHERE ss.DateSortie >= @StartDate AND ss.DateSortie < @EndDate
              AND UPPER(ISNULL(ss.Source,'')) = 'VENTE'
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
        var totals = await LoadTotalsAsync(cn, start, end, ct);

        var response = new AnnuelDashboardResponse
        {
            Year = year,
            CaAnnuel = totals.MontantTotalGenere,
            DepensesAnnuelles = await ScalarDecimalAsync(cn, """
                SELECT ISNULL(SUM(ISNULL(d.Montant,0)),0)
                FROM Depenses d
                WHERE d.DateDepense >= @StartDate AND d.DateDepense < @EndDate
                """, ct, ("@StartDate", start), ("@EndDate", end))
        };
        response.BeneficeEstime = response.CaAnnuel - response.DepensesAnnuelles;
        ApplyTotals(response, totals);
        response.VentesParMois = await QueryMonthSeriesAsync(cn, """
            SELECT RIGHT('0' + CAST(MONTH(ss.DateSortie) AS varchar(2)), 2) AS Label, ISNULL(SUM(ISNULL(ss.MontantLigne,0)),0) AS Value
            FROM StockSortie ss
            WHERE ss.DateSortie >= @StartDate AND ss.DateSortie < @EndDate
              AND UPPER(ISNULL(ss.Source,'')) = 'VENTE'
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
        response.BeneficesParMois = BuildMonthlyProfitSeries(response.VentesParMois, response.DepensesParMois);
        response.CategoriesDepensesGourmandes = await QueryExpensesByCategoryAsync(cn, start, ct, end);
        response.TopProduits = await QueryMonthlyTopProductsAsync(cn, start, end, ct);
        return response;
    }

    private static void ApplyTotals(JournalierDashboardResponse response, DashboardTotals totals)
    {
        response.TotalEntrees = totals.TotalEntrees;
        response.TotalVentes = totals.TotalVentes;
        response.TotalSortiesManuelles = totals.TotalSortiesManuelles;
        response.TotalPertes = totals.TotalPertes;
        response.TotalDons = totals.TotalDons;
        response.TotalAllocations = totals.TotalAllocations;
        response.TotalDettesClients = totals.TotalDettesClients;
        response.TotalDettesBoss = totals.TotalDettesBoss;
        response.TotalSortiesHorsCaisse = totals.TotalSortiesHorsCaisse;
        response.TotalGros = totals.TotalGros;
        response.TotalDemi = totals.TotalDemi;
        response.TotalQuart = totals.TotalQuart;
        response.TotalPiece = totals.TotalPiece;
        response.TotalDouzaine = totals.TotalDouzaine;
        response.MontantTotalGenere = totals.MontantTotalGenere;
    }

    private static void ApplyTotals(MensuelDashboardResponse response, DashboardTotals totals)
    {
        response.TotalEntrees = totals.TotalEntrees;
        response.TotalVentes = totals.TotalVentes;
        response.TotalSortiesManuelles = totals.TotalSortiesManuelles;
        response.TotalPertes = totals.TotalPertes;
        response.TotalDons = totals.TotalDons;
        response.TotalAllocations = totals.TotalAllocations;
        response.TotalDettesClients = totals.TotalDettesClients;
        response.TotalDettesBoss = totals.TotalDettesBoss;
        response.TotalSortiesHorsCaisse = totals.TotalSortiesHorsCaisse;
        response.TotalGros = totals.TotalGros;
        response.TotalDemi = totals.TotalDemi;
        response.TotalQuart = totals.TotalQuart;
        response.TotalPiece = totals.TotalPiece;
        response.TotalDouzaine = totals.TotalDouzaine;
        response.MontantTotalGenere = totals.MontantTotalGenere;
    }

    private static void ApplyTotals(AnnuelDashboardResponse response, DashboardTotals totals)
    {
        response.TotalEntrees = totals.TotalEntrees;
        response.TotalVentes = totals.TotalVentes;
        response.TotalSortiesManuelles = totals.TotalSortiesManuelles;
        response.TotalPertes = totals.TotalPertes;
        response.TotalDons = totals.TotalDons;
        response.TotalAllocations = totals.TotalAllocations;
        response.TotalDettesClients = totals.TotalDettesClients;
        response.TotalDettesBoss = totals.TotalDettesBoss;
        response.TotalSortiesHorsCaisse = totals.TotalSortiesHorsCaisse;
        response.TotalGros = totals.TotalGros;
        response.TotalDemi = totals.TotalDemi;
        response.TotalQuart = totals.TotalQuart;
        response.TotalPiece = totals.TotalPiece;
        response.TotalDouzaine = totals.TotalDouzaine;
        response.MontantTotalGenere = totals.MontantTotalGenere;
    }

    private static List<PeriodSeriesPoint> BuildMonthlyProfitSeries(IReadOnlyList<PeriodSeriesPoint> ventes, IReadOnlyList<PeriodSeriesPoint> depenses)
    {
        var result = new List<PeriodSeriesPoint>();
        var ventesByLabel = ventes.ToDictionary(v => v.Label, v => v.Value, StringComparer.OrdinalIgnoreCase);
        var depensesByLabel = depenses.ToDictionary(v => v.Label, v => v.Value, StringComparer.OrdinalIgnoreCase);
        var labels = ventes.Select(v => v.Label).Concat(depenses.Select(d => d.Label)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToList();
        foreach (var label in labels)
        {
            ventesByLabel.TryGetValue(label, out var vente);
            depensesByLabel.TryGetValue(label, out var depense);
            result.Add(new PeriodSeriesPoint { Label = label, Value = vente - depense });
        }
        return result;
    }

    private async Task<DashboardTotals> LoadTotalsAsync(SqlConnection cn, DateTime start, DateTime end, CancellationToken ct)
    {
        const string sql = """
            WITH Sorties AS (
                SELECT
                    ss.QuantiteBase,
                    ss.MontantLigne,
                    ss.TypeVente,
                    ss.Source,
                    ss.StatutPaiement,
                    ss.ClientId,
                    ss.RefSource,
                    ISNULL(m.Libelle, '') AS MotifLibelle,
                    ISNULL(m.Nature, '') AS MotifNature
                FROM StockSortie ss
                LEFT JOIN MotifSortie m ON m.MotifId = ss.MotifId
                WHERE ss.DateSortie >= @StartDate
                  AND ss.DateSortie < @EndDate
            ),
            Entrees AS (
                SELECT ISNULL(SUM(ISNULL(QuantiteBase,0)),0) AS TotalEntrees
                FROM StockEntree
                WHERE DateEntree >= @StartDate AND DateEntree < @EndDate
            ),
            Pertes AS (
                SELECT ISNULL(SUM(ISNULL(QuantiteBase,0)),0) AS TotalPertes
                FROM StockPerte
                WHERE DatePerte >= @StartDate AND DatePerte < @EndDate
            ),
            Aggregats AS (
                SELECT
                    ISNULL(SUM(CASE WHEN UPPER(ISNULL(Source,'')) = 'VENTE' OR UPPER(ISNULL(RefSource,'')) LIKE 'FACTURE:%' THEN ISNULL(QuantiteBase,0) ELSE 0 END),0) AS TotalVentes,
                    ISNULL(SUM(CASE WHEN UPPER(ISNULL(Source,'')) IN ('SORTIE_MANUELLE','MANUEL','ADMIN') THEN ISNULL(QuantiteBase,0) ELSE 0 END),0) AS TotalSortiesManuelles,
                    ISNULL(SUM(CASE WHEN UPPER(ISNULL(TypeVente,'')) = 'GROS' THEN ISNULL(QuantiteBase,0) ELSE 0 END),0) AS TotalGros,
                    ISNULL(SUM(CASE WHEN UPPER(ISNULL(TypeVente,'')) = 'DEMI' THEN ISNULL(QuantiteBase,0) ELSE 0 END),0) AS TotalDemi,
                    ISNULL(SUM(CASE WHEN UPPER(ISNULL(TypeVente,'')) = 'QUART' THEN ISNULL(QuantiteBase,0) ELSE 0 END),0) AS TotalQuart,
                    ISNULL(SUM(CASE WHEN UPPER(ISNULL(TypeVente,'')) IN ('PIECE','UNITE') THEN ISNULL(QuantiteBase,0) ELSE 0 END),0) AS TotalPiece,
                    ISNULL(SUM(CASE WHEN UPPER(ISNULL(TypeVente,'')) = 'DOUZAINE' THEN ISNULL(QuantiteBase,0) ELSE 0 END),0) AS TotalDouzaine,
                    ISNULL(SUM(CASE WHEN UPPER(ISNULL(MotifLibelle,'')) LIKE '%DON%' OR UPPER(ISNULL(MotifNature,'')) LIKE '%DON%' OR UPPER(ISNULL(MotifLibelle,'')) LIKE '%ECHANTILLON%' THEN ISNULL(QuantiteBase,0) ELSE 0 END),0) AS TotalDons,
                    ISNULL(SUM(CASE WHEN UPPER(ISNULL(MotifLibelle,'')) LIKE '%ALLOC%' OR UPPER(ISNULL(MotifNature,'')) LIKE '%ALLOC%' THEN ISNULL(QuantiteBase,0) ELSE 0 END),0) AS TotalAllocations,
                    ISNULL(SUM(CASE WHEN (UPPER(ISNULL(MotifLibelle,'')) LIKE '%DETTE%' OR UPPER(ISNULL(MotifNature,'')) LIKE '%DETTE%') AND (UPPER(ISNULL(MotifLibelle,'')) LIKE '%CLIENT%' OR ISNULL(ClientId, 0) > 0 OR UPPER(ISNULL(StatutPaiement,'')) = 'IMPAYE') THEN ISNULL(QuantiteBase,0) ELSE 0 END),0) AS TotalDettesClients,
                    ISNULL(SUM(CASE WHEN (UPPER(ISNULL(MotifLibelle,'')) LIKE '%DETTE%' OR UPPER(ISNULL(MotifNature,'')) LIKE '%DETTE%' OR UPPER(ISNULL(MotifLibelle,'')) LIKE '%ORDRE PATRON%') AND (UPPER(ISNULL(MotifLibelle,'')) LIKE '%BOSS%' OR UPPER(ISNULL(MotifLibelle,'')) LIKE '%PATRON%' OR UPPER(ISNULL(MotifLibelle,'')) LIKE '%MAISON%') THEN ISNULL(QuantiteBase,0) ELSE 0 END),0) AS TotalDettesBoss,
                    ISNULL(SUM(CASE WHEN UPPER(ISNULL(Source,'')) = 'SORTIE_MANUELLE' AND (UPPER(ISNULL(MotifLibelle,'')) LIKE '%HORS%' OR UPPER(ISNULL(MotifNature,'')) LIKE '%HORS%') THEN ISNULL(QuantiteBase,0) ELSE 0 END),0) AS TotalSortiesHorsCaisse,
                    ISNULL(SUM(CASE
                        WHEN UPPER(ISNULL(StatutPaiement,'')) = 'GRATUIT' THEN 0
                        WHEN UPPER(ISNULL(MotifLibelle,'')) LIKE '%DON%' OR UPPER(ISNULL(MotifNature,'')) LIKE '%DON%' THEN 0
                        WHEN UPPER(ISNULL(MotifLibelle,'')) LIKE '%ECHANTILLON%' THEN 0
                        WHEN UPPER(ISNULL(MotifLibelle,'')) LIKE '%PERTE%' OR UPPER(ISNULL(MotifNature,'')) LIKE '%PERTE%' THEN 0
                        WHEN UPPER(ISNULL(MotifLibelle,'')) LIKE '%CASSE%' OR UPPER(ISNULL(MotifNature,'')) LIKE '%CASSE%' THEN 0
                        WHEN UPPER(ISNULL(MotifLibelle,'')) LIKE '%VOL%' OR UPPER(ISNULL(MotifNature,'')) LIKE '%VOL%' THEN 0
                        WHEN UPPER(ISNULL(MotifLibelle,'')) LIKE '%ALLOC%' OR UPPER(ISNULL(MotifNature,'')) LIKE '%ALLOC%' THEN 0
                        ELSE ISNULL(MontantLigne,0)
                    END),0) AS MontantTotalGenere
                FROM Sorties
            )
            SELECT
                e.TotalEntrees,
                p.TotalPertes,
                a.TotalVentes,
                a.TotalSortiesManuelles,
                a.TotalGros,
                a.TotalDemi,
                a.TotalQuart,
                a.TotalPiece,
                a.TotalDouzaine,
                a.TotalDons,
                a.TotalAllocations,
                a.TotalDettesClients,
                a.TotalDettesBoss,
                a.TotalSortiesHorsCaisse,
                a.MontantTotalGenere
            FROM Entrees e
            CROSS JOIN Pertes p
            CROSS JOIN Aggregats a
            """;
        await using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@StartDate", start);
        cmd.Parameters.AddWithValue("@EndDate", end);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct))
        {
            return new DashboardTotals();
        }

        return new DashboardTotals
        {
            TotalEntrees = reader.IsDBNull(0) ? 0m : Convert.ToDecimal(reader.GetValue(0)),
            TotalPertes = reader.IsDBNull(1) ? 0m : Convert.ToDecimal(reader.GetValue(1)),
            TotalVentes = reader.IsDBNull(2) ? 0m : Convert.ToDecimal(reader.GetValue(2)),
            TotalSortiesManuelles = reader.IsDBNull(3) ? 0m : Convert.ToDecimal(reader.GetValue(3)),
            TotalGros = reader.IsDBNull(4) ? 0m : Convert.ToDecimal(reader.GetValue(4)),
            TotalDemi = reader.IsDBNull(5) ? 0m : Convert.ToDecimal(reader.GetValue(5)),
            TotalQuart = reader.IsDBNull(6) ? 0m : Convert.ToDecimal(reader.GetValue(6)),
            TotalPiece = reader.IsDBNull(7) ? 0m : Convert.ToDecimal(reader.GetValue(7)),
            TotalDouzaine = reader.IsDBNull(8) ? 0m : Convert.ToDecimal(reader.GetValue(8)),
            TotalDons = reader.IsDBNull(9) ? 0m : Convert.ToDecimal(reader.GetValue(9)),
            TotalAllocations = reader.IsDBNull(10) ? 0m : Convert.ToDecimal(reader.GetValue(10)),
            TotalDettesClients = reader.IsDBNull(11) ? 0m : Convert.ToDecimal(reader.GetValue(11)),
            TotalDettesBoss = reader.IsDBNull(12) ? 0m : Convert.ToDecimal(reader.GetValue(12)),
            TotalSortiesHorsCaisse = reader.IsDBNull(13) ? 0m : Convert.ToDecimal(reader.GetValue(13)),
            MontantTotalGenere = reader.IsDBNull(14) ? 0m : Convert.ToDecimal(reader.GetValue(14))
        };
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
              AND UPPER(ISNULL(ss.Source,'')) = 'VENTE'
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
              AND UPPER(ISNULL(ss.Source,'')) IN ('SORTIE_MANUELLE','MANUEL','ADMIN')
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
              AND UPPER(ISNULL(ss.Source,'')) = 'VENTE'
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

    private sealed record DashboardTotals
    {
        public decimal TotalEntrees { get; init; }
        public decimal TotalVentes { get; init; }
        public decimal TotalSortiesManuelles { get; init; }
        public decimal TotalPertes { get; init; }
        public decimal TotalDons { get; init; }
        public decimal TotalAllocations { get; init; }
        public decimal TotalDettesClients { get; init; }
        public decimal TotalDettesBoss { get; init; }
        public decimal TotalSortiesHorsCaisse { get; init; }
        public decimal TotalGros { get; init; }
        public decimal TotalDemi { get; init; }
        public decimal TotalQuart { get; init; }
        public decimal TotalPiece { get; init; }
        public decimal TotalDouzaine { get; init; }
        public decimal MontantTotalGenere { get; init; }
    }
}
