using CommercialMagDb.Api.Contracts.Dashboard;
using System.Data;
using Microsoft.Data.SqlClient;

namespace CommercialMagDb.Api.Infrastructure;

public sealed class DashboardRepository(DbConnectionFactory factory)
{
    public async Task<JournalierDashboardResponse> GetJournalierAsync(DateTime date, CancellationToken ct = default)
        => await GetJournalierAsync(date.Date, date.Date, ct);

    public async Task<JournalierDashboardResponse> GetJournalierAsync(DateTime startDate, DateTime endDate, CancellationToken ct = default)
    {
        await using var cn = factory.Create();
        await cn.OpenAsync(ct);
        var start = startDate.Date;
        var end = endDate.Date.AddDays(1);
        var totals = await LoadTotalsAsync(cn, start, end, ct);

        var response = new JournalierDashboardResponse
        {
            Date = start,
            CaDuJour = totals.MontantTotalGenere,
            TotalSorties = totals.TotalVentes + totals.TotalSortiesManuelles + totals.TotalPertes,
            DepensesDuJour = await ScalarDecimalAsync(cn, """
                SELECT ISNULL(SUM(ISNULL(d.Montant,0)),0)
                FROM Depenses d
                WHERE d.DateDepense >= @StartDate
                  AND d.DateDepense < @EndDate
                """, ct, ("@StartDate", start), ("@EndDate", end))
        };
        response.BeneficeEstime = response.CaDuJour - response.DepensesDuJour;
        ApplyTotals(response, totals);

        response.ProduitsVendus = await QueryDailyProductsAsync(cn, start, end, ct);
        response.SortiesManuelles = await QueryManualExitsAsync(cn, start, end, ct);
        response.DepensesParCategorie = await QueryExpensesByCategoryAsync(cn, start, ct, end);
        response.AlertesStockFaible = await QueryStockAlertsAsync(cn, ct);
        response.SeriesVentes = await QuerySeriesAsync(cn, """
            SELECT CONVERT(date, f.CreeLe) AS Label,
                   ISNULL(SUM(CASE WHEN ISNULL(l.MontantLigne, 0) <> 0 THEN l.MontantLigne ELSE ISNULL(l.QuantiteSaisie, 0) * ISNULL(l.PrixUnitaire, 0) END), 0) AS Value
            FROM LignesFactureVente l
            INNER JOIN FacturesVente f ON f.FactureVenteId = l.FactureVenteId
            WHERE f.CreeLe >= @StartDate
              AND f.CreeLe < @EndDate
              AND UPPER(ISNULL(f.Statut,'')) = 'PAYEE'
            GROUP BY CONVERT(date, f.CreeLe)
            ORDER BY CONVERT(date, f.CreeLe)
            """, ct, ("@StartDate", start), ("@EndDate", end));
        response.SeriesDepenses = await QuerySeriesAsync(cn, """
            SELECT CONVERT(date, d.DateDepense) AS Label, ISNULL(SUM(ISNULL(d.Montant,0)),0) AS Value
            FROM Depenses d
            WHERE d.DateDepense >= @StartDate
              AND d.DateDepense < @EndDate
            GROUP BY CONVERT(date, d.DateDepense)
            ORDER BY CONVERT(date, d.DateDepense)
            """, ct, ("@StartDate", start), ("@EndDate", end));
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
            SELECT CONVERT(date, f.CreeLe) AS Label,
                   ISNULL(SUM(CASE WHEN ISNULL(l.MontantLigne, 0) <> 0 THEN l.MontantLigne ELSE ISNULL(l.QuantiteSaisie, 0) * ISNULL(l.PrixUnitaire, 0) END), 0) AS Value
            FROM LignesFactureVente l
            INNER JOIN FacturesVente f ON f.FactureVenteId = l.FactureVenteId
            WHERE f.CreeLe >= @StartDate AND f.CreeLe < @EndDate
              AND UPPER(ISNULL(f.Statut,'')) = 'PAYEE'
            GROUP BY CONVERT(date, f.CreeLe)
            ORDER BY CONVERT(date, f.CreeLe)
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
            SELECT RIGHT('0' + CAST(MONTH(f.CreeLe) AS varchar(2)), 2) AS Label,
                   ISNULL(SUM(CASE WHEN ISNULL(l.MontantLigne, 0) <> 0 THEN l.MontantLigne ELSE ISNULL(l.QuantiteSaisie, 0) * ISNULL(l.PrixUnitaire, 0) END), 0) AS Value
            FROM LignesFactureVente l
            INNER JOIN FacturesVente f ON f.FactureVenteId = l.FactureVenteId
            WHERE f.CreeLe >= @StartDate AND f.CreeLe < @EndDate
              AND UPPER(ISNULL(f.Statut,'')) = 'PAYEE'
            GROUP BY MONTH(f.CreeLe)
            ORDER BY MONTH(f.CreeLe)
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

    public async Task<AnalyseVenteResponse> GetAnalyseVenteAsync(DateTime dateDebut, DateTime dateFin, CancellationToken ct = default)
    {
        await using var cn = factory.Create();
        await cn.OpenAsync(ct);

        const string sql = """
            WITH CTEStockEntree AS
            (
                SELECT se.ProduitId,
                       SUM(ISNULL(se.QuantiteBase, 0)) AS QuantiteEntreePieces,
                       SUM(ISNULL(se.QuantiteSaisie, 0) * ISNULL(se.PrixAchat, 0)) AS ValeurStockEntree,
                       SUM(ISNULL(se.QuantiteSaisie, 0) * ISNULL(se.PrixAchat, 0))
                       / NULLIF(SUM(ISNULL(se.QuantiteBase, 0)), 0) AS CoutAchatMoyenPiece
                FROM StockEntree se
                WHERE se.DateEntree >= @DateDebut
                  AND se.DateEntree < DATEADD(DAY, 1, @DateFin)
                  AND (se.IdStock LIKE 'ENT%' OR se.IdStock LIKE 'INIT%')
                GROUP BY se.ProduitId
            ),
            CoutProduit AS
            (
                SELECT se.ProduitId,
                       SUM(ISNULL(se.QuantiteSaisie, 0) * ISNULL(se.PrixAchat, 0)) / NULLIF(SUM(ISNULL(se.QuantiteBase, 0)), 0) AS CoutMoyenPiece
                FROM StockEntree se
                WHERE se.IdStock LIKE 'ENT%' OR se.IdStock LIKE 'INIT%'
                GROUP BY se.ProduitId
            ),
            Ventes AS
            (
                SELECT l.ProduitId,
                       SUM(ISNULL(l.Quantite, 0)) AS QuantiteVenduePieces,
                       SUM(ISNULL(l.MontantLigne, ISNULL(l.QuantiteSaisie, 0) * ISNULL(l.PrixUnitaire, 0))) AS ChiffreAffaires
                FROM LignesFactureVente l
                INNER JOIN FacturesVente f ON f.FactureVenteId = l.FactureVenteId
                WHERE f.Statut = 'PAYEE'
                  AND f.CreeLe >= @DateDebut
                  AND f.CreeLe < DATEADD(DAY, 1, @DateFin)
                GROUP BY l.ProduitId
            ),
            DepensesPeriode AS
            (
                SELECT ISNULL(SUM(ISNULL(Montant, 0)), 0) AS TotalDepenses
                FROM Depenses
                WHERE DateDepense >= @DateDebut
                  AND DateDepense < DATEADD(DAY, 1, @DateFin)
            ),
            SortiesManuelles AS
            (
                SELECT ISNULL(SUM(ISNULL(ss.QuantiteBase, 0) * ISNULL(cp.CoutMoyenPiece, 0)), 0) AS TotalChargesManuelles
                FROM StockSortie ss
                LEFT JOIN CoutProduit cp ON cp.ProduitId = ss.ProduitId
                WHERE ss.DateSortie >= @DateDebut
                  AND ss.DateSortie < DATEADD(DAY, 1, @DateFin)
                  AND UPPER(ISNULL(ss.Source, '')) IN ('SORTIE_MANUELLE', 'MANUEL')
            ),
            AnalyseProduit AS
            (
                SELECT p.ProduitId, p.Libelle AS Produit,
                       ISNULL(se.ValeurStockEntree, 0) AS ValeurStockEntree,
                       ISNULL(v.QuantiteVenduePieces, 0) AS QuantiteVenduePieces,
                       ISNULL(v.ChiffreAffaires, 0) AS ChiffreAffaires,
                       ISNULL(v.QuantiteVenduePieces, 0) * ISNULL(cp.CoutMoyenPiece, 0) AS CoutMarchandisesVendues,
                       ISNULL(v.ChiffreAffaires, 0) - (ISNULL(v.QuantiteVenduePieces, 0) * ISNULL(cp.CoutMoyenPiece, 0)) AS Benefice,
                       ISNULL(s.QuantiteStock, 0) AS StockRestantPieces,
                       ISNULL(s.QuantiteStock, 0) * ISNULL(cp.CoutMoyenPiece, 0) AS CoutStockRestant
                FROM Produits p
                LEFT JOIN CTEStockEntree se ON se.ProduitId = p.ProduitId
                LEFT JOIN CoutProduit cp ON cp.ProduitId = p.ProduitId
                LEFT JOIN Ventes v ON v.ProduitId = p.ProduitId
                LEFT JOIN vStockProduit s ON s.ProduitId = p.ProduitId
            )
            SELECT ISNULL(CAST(SUM(ValeurStockEntree) AS BIGINT), 0) AS ValeurStockEntree,
                   ISNULL(CAST(SUM(CoutMarchandisesVendues) AS BIGINT), 0) AS CoutMarchandisesVendues,
                   ISNULL(CAST(SUM(ChiffreAffaires) AS BIGINT), 0) AS ChiffreAffaires,
                   ISNULL(CAST(SUM(Benefice) AS BIGINT), 0) AS BeneficeRealise,
                   ISNULL(CAST(MAX(dp.TotalDepenses) AS BIGINT), 0) AS DepensesTotal,
                   ISNULL(CAST(MAX(sm.TotalChargesManuelles) AS BIGINT), 0) AS ChargesSortiesManuelles,
                   ISNULL(CAST(SUM(Benefice) - MAX(dp.TotalDepenses) - MAX(sm.TotalChargesManuelles) AS BIGINT), 0) AS BeneficeNetRealise,
                   ISNULL(CAST(SUM(CoutStockRestant) AS BIGINT), 0) AS CoutStockRestant,
                   ISNULL(CAST(SUM(CoutStockRestant) * (ISNULL(SUM(Benefice), 0) / NULLIF(ISNULL(SUM(CoutMarchandisesVendues), 0), 0)) AS BIGINT), 0) AS ProjectionBeneficeRestant,
                   ISNULL(CAST(((ISNULL(SUM(Benefice), 0) - MAX(dp.TotalDepenses) - MAX(sm.TotalChargesManuelles)) * 100.0 / NULLIF(ISNULL(SUM(CoutMarchandisesVendues), 0), 0)) AS DECIMAL(10,2)), 0) AS MargeBeneficiairePourcentage,
                   CASE
                       WHEN ISNULL(SUM(Benefice), 0) - MAX(dp.TotalDepenses) - MAX(sm.TotalChargesManuelles) < 0 THEN 'CRITIQUE / PERTE'
                       WHEN ISNULL(SUM(Benefice), 0) - MAX(dp.TotalDepenses) - MAX(sm.TotalChargesManuelles) = 0 THEN 'POINT MORT'
                       WHEN (ISNULL(SUM(Benefice), 0) - MAX(dp.TotalDepenses) - MAX(sm.TotalChargesManuelles)) * 100.0 / NULLIF(ISNULL(SUM(CoutMarchandisesVendues), 0), 0) < 10 THEN 'FAIBLE RENTABILITÉ'
                       WHEN (ISNULL(SUM(Benefice), 0) - MAX(dp.TotalDepenses) - MAX(sm.TotalChargesManuelles)) * 100.0 / NULLIF(ISNULL(SUM(CoutMarchandisesVendues), 0), 0) BETWEEN 10 AND 25 THEN 'PROGRÈS'
                       ELSE 'BONNE RENTABILITÉ'
                   END AS Evaluation
            FROM AnalyseProduit
            CROSS JOIN DepensesPeriode dp
            CROSS JOIN SortiesManuelles sm;
            """;

        await using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@DateDebut", dateDebut.Date);
        cmd.Parameters.AddWithValue("@DateFin", dateFin.Date);

        var response = new AnalyseVenteResponse
        {
            DateDebut = dateDebut.Date,
            DateFin = dateFin.Date,
            PeriodeLabel = $"{dateDebut:dd/MM/yyyy} au {dateFin:dd/MM/yyyy}"
        };

        await using (var reader = await cmd.ExecuteReaderAsync(ct))
        {
            if (await reader.ReadAsync(ct))
            {
                response.ValeurStockEntree = ReadDecimal(reader, 0);
                response.CoutMarchandisesVendues = ReadDecimal(reader, 1);
                response.ChiffreAffaires = ReadDecimal(reader, 2);
                response.BeneficeRealise = ReadDecimal(reader, 3);
                response.DepensesTotal = ReadDecimal(reader, 4);
                response.ChargesSortiesManuelles = ReadDecimal(reader, 5);
                response.BeneficeNetRealise = ReadDecimal(reader, 6);
                response.CoutStockRestant = ReadDecimal(reader, 7);
                response.ProjectionBeneficeRestant = ReadDecimal(reader, 8);
                response.MargeBeneficiairePourcentage = ReadDecimal(reader, 9);
                response.Evaluation = reader.IsDBNull(10) ? string.Empty : reader.GetValue(10)?.ToString() ?? string.Empty;
            }
        }

        response.Details = await QueryAnalyseVenteDetailsAsync(cn, dateDebut, dateFin, ct);
        response.Details.Insert(0, new AnalyseVenteDetailRow
        {
            Ordre = 0,
            Rubrique = "Synthèse",
            Categorie = "Bénéfice réalisé",
            QuantitePieces = 0m,
            Montant = response.BeneficeRealise,
            Commentaire = "Résultat commercial avant charges"
        });
        response.Details.Insert(1, new AnalyseVenteDetailRow
        {
            Ordre = 1,
            Rubrique = "Synthèse",
            Categorie = "Dépenses",
            QuantitePieces = 0m,
            Montant = response.DepensesTotal,
            Commentaire = "Dépenses de la période"
        });
        response.Details.Insert(2, new AnalyseVenteDetailRow
        {
            Ordre = 2,
            Rubrique = "Synthèse",
            Categorie = "Sorties manuelles",
            QuantitePieces = 0m,
            Montant = response.ChargesSortiesManuelles,
            Commentaire = "Sorties valorisées au coût réel"
        });
        response.Details.Add(new AnalyseVenteDetailRow
        {
            Ordre = 99,
            Rubrique = "Synthèse",
            Categorie = "Bénéfice net réalisé",
            QuantitePieces = 0m,
            Montant = response.BeneficeNetRealise,
            Commentaire = "Bénéfice après déductions"
        });
        response.Details = response.Details.OrderBy(x => x.Ordre).ToList();
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
            WITH Entrees AS (
                SELECT ISNULL(SUM(ISNULL(QuantiteBase,0)),0) AS TotalEntrees
                FROM StockEntree
                WHERE DateEntree >= @StartDate AND DateEntree < @EndDate
            ),
            Pertes AS (
                SELECT ISNULL(SUM(ISNULL(QuantiteBase,0)),0) AS TotalPertes
                FROM StockPerte
                WHERE DatePerte >= @StartDate AND DatePerte < @EndDate
            ),
            Ventes AS (
                SELECT
                    ISNULL(SUM(ISNULL(l.Quantite,0)),0) AS TotalVentes,
                    ISNULL(SUM(CASE WHEN UPPER(ISNULL(l.TypeVente,'')) = 'GROS' THEN ISNULL(l.Quantite,0) ELSE 0 END),0) AS TotalGros,
                    ISNULL(SUM(CASE WHEN UPPER(ISNULL(l.TypeVente,'')) = 'DEMI' THEN ISNULL(l.Quantite,0) ELSE 0 END),0) AS TotalDemi,
                    ISNULL(SUM(CASE WHEN UPPER(ISNULL(l.TypeVente,'')) = 'QUART' THEN ISNULL(l.Quantite,0) ELSE 0 END),0) AS TotalQuart,
                    ISNULL(SUM(CASE WHEN UPPER(ISNULL(l.TypeVente,'')) IN ('PIECE','UNITE') THEN ISNULL(l.Quantite,0) ELSE 0 END),0) AS TotalPiece,
                    ISNULL(SUM(CASE WHEN UPPER(ISNULL(l.TypeVente,'')) = 'DOUZAINE' THEN ISNULL(l.Quantite,0) ELSE 0 END),0) AS TotalDouzaine,
                    ISNULL(SUM(CASE WHEN ISNULL(l.MontantLigne, 0) <> 0 THEN l.MontantLigne ELSE ISNULL(l.QuantiteSaisie,0) * ISNULL(l.PrixUnitaire,0) END),0) AS MontantTotalGenere
                FROM LignesFactureVente l
                INNER JOIN FacturesVente f ON f.FactureVenteId = l.FactureVenteId
                WHERE f.CreeLe >= @StartDate
                  AND f.CreeLe < @EndDate
                  AND UPPER(ISNULL(f.Statut,'')) = 'PAYEE'
            ),
            Sorties AS (
                SELECT
                    ISNULL(SUM(CASE WHEN UPPER(ISNULL(ss.Source,'')) IN ('SORTIE_MANUELLE','MANUEL','ADMIN') THEN ISNULL(ss.QuantiteBase,0) ELSE 0 END),0) AS TotalSortiesManuelles,
                    ISNULL(SUM(CASE WHEN UPPER(ISNULL(ss.Source,'')) = 'SORTIE_MANUELLE' AND (UPPER(ISNULL(m.Libelle,'')) LIKE '%HORS%' OR UPPER(ISNULL(m.Nature,'')) LIKE '%HORS%') THEN ISNULL(ss.QuantiteBase,0) ELSE 0 END),0) AS TotalSortiesHorsCaisse,
                    ISNULL(SUM(CASE WHEN UPPER(ISNULL(m.Libelle,'')) LIKE '%DON%' OR UPPER(ISNULL(m.Nature,'')) LIKE '%DON%' OR UPPER(ISNULL(m.Libelle,'')) LIKE '%ECHANTILLON%' THEN ISNULL(ss.QuantiteBase,0) ELSE 0 END),0) AS TotalDons,
                    ISNULL(SUM(CASE WHEN UPPER(ISNULL(m.Libelle,'')) LIKE '%ALLOC%' OR UPPER(ISNULL(m.Nature,'')) LIKE '%ALLOC%' THEN ISNULL(ss.QuantiteBase,0) ELSE 0 END),0) AS TotalAllocations,
                    ISNULL(SUM(CASE WHEN (UPPER(ISNULL(m.Libelle,'')) LIKE '%DETTE%' OR UPPER(ISNULL(m.Nature,'')) LIKE '%DETTE%') AND (UPPER(ISNULL(m.Libelle,'')) LIKE '%CLIENT%' OR ISNULL(ss.ClientId, 0) > 0 OR UPPER(ISNULL(ss.StatutPaiement,'')) = 'IMPAYE') THEN ISNULL(ss.QuantiteBase,0) ELSE 0 END),0) AS TotalDettesClients,
                    ISNULL(SUM(CASE WHEN (UPPER(ISNULL(m.Libelle,'')) LIKE '%DETTE%' OR UPPER(ISNULL(m.Nature,'')) LIKE '%DETTE%' OR UPPER(ISNULL(m.Libelle,'')) LIKE '%ORDRE PATRON%') AND (UPPER(ISNULL(m.Libelle,'')) LIKE '%BOSS%' OR UPPER(ISNULL(m.Libelle,'')) LIKE '%PATRON%' OR UPPER(ISNULL(m.Libelle,'')) LIKE '%MAISON%') THEN ISNULL(ss.QuantiteBase,0) ELSE 0 END),0) AS TotalDettesBoss
                FROM StockSortie ss
                LEFT JOIN MotifSortie m ON m.MotifId = ss.MotifId
                WHERE ss.DateSortie >= @StartDate
                  AND ss.DateSortie < @EndDate
            )
            SELECT
                e.TotalEntrees,
                p.TotalPertes,
                v.TotalVentes,
                s.TotalSortiesManuelles,
                v.TotalGros,
                v.TotalDemi,
                v.TotalQuart,
                v.TotalPiece,
                v.TotalDouzaine,
                s.TotalDons,
                s.TotalAllocations,
                s.TotalDettesClients,
                s.TotalDettesBoss,
                s.TotalSortiesHorsCaisse,
                v.MontantTotalGenere
            FROM Entrees e
            CROSS JOIN Pertes p
            CROSS JOIN Ventes v
            CROSS JOIN Sorties s
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

    private static async Task<List<DailyProductRow>> QueryDailyProductsAsync(SqlConnection cn, DateTime start, DateTime end, CancellationToken ct)
    {
        const string sql = """
            SELECT TOP 10 p.Libelle,
                  ISNULL(SUM(ISNULL(l.Quantite,0)),0) AS Quantite,
                   ISNULL(MAX(ISNULL(l.TypeVente,'')), '') AS TypeVente,
                   ISNULL(SUM(CASE WHEN ISNULL(l.MontantLigne, 0) <> 0 THEN l.MontantLigne ELSE ISNULL(l.QuantiteSaisie,0) * ISNULL(l.PrixUnitaire,0) END),0) AS Montant,
                   ISNULL(MIN(f.CreeLe), GETDATE()) AS Heure,
                   ISNULL(MAX(u.NomUtilisateur), '') AS Agent
            FROM LignesFactureVente l
            INNER JOIN FacturesVente f ON f.FactureVenteId = l.FactureVenteId
            INNER JOIN Produits p ON p.ProduitId = l.ProduitId
            LEFT JOIN Utilisateurs u ON u.UtilisateurId = f.CreePar
            WHERE f.CreeLe >= @StartDate
              AND f.CreeLe < @EndDate
              AND UPPER(ISNULL(f.Statut,'')) = 'PAYEE'
            GROUP BY p.Libelle
            ORDER BY SUM(CASE WHEN ISNULL(l.MontantLigne, 0) <> 0 THEN l.MontantLigne ELSE ISNULL(l.QuantiteSaisie,0) * ISNULL(l.PrixUnitaire,0) END) DESC
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
                Product = reader.GetString(0),
                QuantitySold = reader.IsDBNull(1) ? 0m : Convert.ToDecimal(reader.GetValue(1)),
                TypeVente = reader.GetValue(2).ToString() ?? string.Empty,
                AmountGenerated = reader.IsDBNull(3) ? 0m : Convert.ToDecimal(reader.GetValue(3)),
                Hour = reader.IsDBNull(4) ? start : Convert.ToDateTime(reader.GetValue(4)),
                Agent = reader.GetValue(5).ToString() ?? string.Empty
            });
        }
        return list;
    }

    private static async Task<List<ManualExitRow>> QueryManualExitsAsync(SqlConnection cn, DateTime start, DateTime end, CancellationToken ct)
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
            WHERE ss.DateSortie >= @StartDate
              AND ss.DateSortie < @EndDate
              AND UPPER(ISNULL(ss.Source,'')) IN ('SORTIE_MANUELLE','MANUEL','ADMIN')
            ORDER BY ss.DateSortie DESC
            """;
        await using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@StartDate", start);
        cmd.Parameters.AddWithValue("@EndDate", end);
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
                Date = reader.IsDBNull(5) ? start : Convert.ToDateTime(reader.GetValue(5)),
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
                  ISNULL(SUM(ISNULL(l.Quantite,0)),0) AS Quantite,
                   ISNULL(MAX(ISNULL(l.TypeVente,'')), '') AS TypeVente,
                   ISNULL(SUM(CASE WHEN ISNULL(l.MontantLigne, 0) <> 0 THEN l.MontantLigne ELSE ISNULL(l.QuantiteSaisie,0) * ISNULL(l.PrixUnitaire,0) END),0) AS Montant,
                   ISNULL(MIN(f.CreeLe), GETDATE()) AS Heure,
                   ISNULL(MAX(u.NomUtilisateur), '') AS Agent
            FROM LignesFactureVente l
            INNER JOIN FacturesVente f ON f.FactureVenteId = l.FactureVenteId
            INNER JOIN Produits p ON p.ProduitId = l.ProduitId
            LEFT JOIN Utilisateurs u ON u.UtilisateurId = f.CreePar
            WHERE f.CreeLe >= @StartDate AND f.CreeLe < @EndDate
              AND UPPER(ISNULL(f.Statut,'')) = 'PAYEE'
            GROUP BY p.Libelle
            ORDER BY SUM(CASE WHEN ISNULL(l.MontantLigne, 0) <> 0 THEN l.MontantLigne ELSE ISNULL(l.QuantiteSaisie,0) * ISNULL(l.PrixUnitaire,0) END) DESC
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

    private static async Task<List<AnalyseVenteDetailRow>> QueryAnalyseVenteDetailsAsync(SqlConnection cn, DateTime dateDebut, DateTime dateFin, CancellationToken ct)
    {
        const string sqlDepenses = """
            SELECT ISNULL(NULLIF(LTRIM(RTRIM(d.Categorie)), ''), 'Sans catégorie') AS Categorie,
                   COUNT(*) AS NombreDepenses,
                   SUM(ISNULL(d.Montant, 0)) AS MontantTotal
            FROM Depenses d
            WHERE d.DateDepense >= @DateDebut
              AND d.DateDepense < DATEADD(DAY, 1, @DateFin)
            GROUP BY ISNULL(NULLIF(LTRIM(RTRIM(d.Categorie)), ''), 'Sans catégorie')
            ORDER BY SUM(ISNULL(d.Montant, 0)) DESC, Categorie ASC
            """;

        const string sqlCharges = """
            WITH CoutPieceProduit AS (
                SELECT se.ProduitId,
                       CASE
                           WHEN ISNULL(p.ConversionUnite, 0) > 0 AND ISNULL(p.PrixAchat, 0) > 0 THEN ISNULL(p.PrixAchat, 0) / NULLIF(ISNULL(p.ConversionUnite, 0), 0)
                           ELSE SUM(ISNULL(se.PrixAchat, 0)) / NULLIF(SUM(ISNULL(se.QuantiteBase, 0)), 0)
                       END AS CoutPiece
                FROM StockEntree se
                INNER JOIN Produits p ON p.ProduitId = se.ProduitId
                WHERE se.DateEntree < DATEADD(DAY, 1, @DateFin)
                GROUP BY se.ProduitId, p.PrixAchat, p.ConversionUnite
            )
            SELECT Categorie, SUM(Pieces) AS QuantitePieces, SUM(Montant) AS MontantTotal
            FROM (
                SELECT 'Sorties gratuites' AS Categorie, ISNULL(ss.QuantiteBase, 0) AS Pieces, ISNULL(ss.QuantiteBase, 0) * ISNULL(cp.CoutPiece, 0) AS Montant
                FROM StockSortie ss
                LEFT JOIN CoutPieceProduit cp ON cp.ProduitId = ss.ProduitId
                WHERE ss.DateSortie >= @DateDebut AND ss.DateSortie < DATEADD(DAY, 1, @DateFin)
                  AND UPPER(ISNULL(ss.Source, '')) IN ('SORTIE_MANUELLE', 'MANUEL')
                  AND UPPER(ISNULL(ss.StatutPaiement, '')) = 'GRATUIT'
                UNION ALL
                SELECT 'Dons' AS Categorie, ISNULL(ss.QuantiteBase, 0) AS Pieces, ISNULL(ss.QuantiteBase, 0) * ISNULL(cp.CoutPiece, 0) AS Montant
                FROM StockSortie ss
                LEFT JOIN MotifSortie m ON m.MotifId = ss.MotifId
                LEFT JOIN CoutPieceProduit cp ON cp.ProduitId = ss.ProduitId
                WHERE ss.DateSortie >= @DateDebut AND ss.DateSortie < DATEADD(DAY, 1, @DateFin)
                  AND UPPER(ISNULL(ss.Source, '')) IN ('SORTIE_MANUELLE', 'MANUEL')
                  AND (UPPER(ISNULL(m.Nature, '')) LIKE '%DON%' OR UPPER(ISNULL(m.Libelle, '')) LIKE '%DON%')
                UNION ALL
                SELECT 'Allocations' AS Categorie, ISNULL(ss.QuantiteBase, 0) AS Pieces, ISNULL(ss.QuantiteBase, 0) * ISNULL(cp.CoutPiece, 0) AS Montant
                FROM StockSortie ss
                LEFT JOIN MotifSortie m ON m.MotifId = ss.MotifId
                LEFT JOIN CoutPieceProduit cp ON cp.ProduitId = ss.ProduitId
                WHERE ss.DateSortie >= @DateDebut AND ss.DateSortie < DATEADD(DAY, 1, @DateFin)
                  AND UPPER(ISNULL(ss.Source, '')) IN ('SORTIE_MANUELLE', 'MANUEL')
                  AND (UPPER(ISNULL(m.Nature, '')) LIKE '%ALLOC%' OR UPPER(ISNULL(m.Libelle, '')) LIKE '%ALLOC%')
                UNION ALL
                SELECT 'Dettes boss' AS Categorie, ISNULL(ss.QuantiteBase, 0) AS Pieces, ISNULL(ss.QuantiteBase, 0) * ISNULL(cp.CoutPiece, 0) AS Montant
                FROM StockSortie ss
                LEFT JOIN MotifSortie m ON m.MotifId = ss.MotifId
                LEFT JOIN CoutPieceProduit cp ON cp.ProduitId = ss.ProduitId
                WHERE ss.DateSortie >= @DateDebut AND ss.DateSortie < DATEADD(DAY, 1, @DateFin)
                  AND UPPER(ISNULL(ss.Source, '')) IN ('SORTIE_MANUELLE', 'MANUEL')
                  AND (UPPER(ISNULL(m.Nature, '')) LIKE '%DETTE%' OR UPPER(ISNULL(m.Libelle, '')) LIKE '%DETTE%')
                  AND (UPPER(ISNULL(m.Libelle, '')) LIKE '%BOSS%' OR UPPER(ISNULL(m.Libelle, '')) LIKE '%PATRON%' OR UPPER(ISNULL(m.Libelle, '')) LIKE '%MAISON%')
                UNION ALL
                SELECT 'Hors caisse' AS Categorie, ISNULL(ss.QuantiteBase, 0) AS Pieces, ISNULL(ss.QuantiteBase, 0) * ISNULL(cp.CoutPiece, 0) AS Montant
                FROM StockSortie ss
                LEFT JOIN MotifSortie m ON m.MotifId = ss.MotifId
                LEFT JOIN CoutPieceProduit cp ON cp.ProduitId = ss.ProduitId
                WHERE ss.DateSortie >= @DateDebut AND ss.DateSortie < DATEADD(DAY, 1, @DateFin)
                  AND UPPER(ISNULL(ss.Source, '')) IN ('SORTIE_MANUELLE', 'MANUEL')
                  AND (UPPER(ISNULL(m.Nature, '')) LIKE '%HORS%' OR UPPER(ISNULL(m.Libelle, '')) LIKE '%HORS%')
                UNION ALL
                SELECT 'Pertes' AS Categorie, ISNULL(sp.QuantiteBase, 0) AS Pieces, ISNULL(sp.QuantiteBase, 0) * ISNULL(cp.CoutPiece, 0) AS Montant
                FROM StockPerte sp
                LEFT JOIN CoutPieceProduit cp ON cp.ProduitId = sp.ProduitId
                WHERE sp.DatePerte >= @DateDebut AND sp.DatePerte < DATEADD(DAY, 1, @DateFin)
            ) q
            GROUP BY Categorie
            ORDER BY SUM(Montant) DESC, Categorie ASC
            """;

        const string sqlCreances = """
            WITH CoutPieceProduit AS (
                SELECT se.ProduitId,
                       CASE
                           WHEN ISNULL(p.ConversionUnite, 0) > 0 AND ISNULL(p.PrixAchat, 0) > 0 THEN ISNULL(p.PrixAchat, 0) / NULLIF(ISNULL(p.ConversionUnite, 0), 0)
                           ELSE SUM(ISNULL(se.PrixAchat, 0)) / NULLIF(SUM(ISNULL(se.QuantiteBase, 0)), 0)
                       END AS CoutPiece
                FROM StockEntree se
                INNER JOIN Produits p ON p.ProduitId = se.ProduitId
                WHERE se.DateEntree < DATEADD(DAY, 1, @DateFin)
                GROUP BY se.ProduitId, p.PrixAchat, p.ConversionUnite
            )
            SELECT 'Créances clients' AS Categorie,
                   ISNULL(SUM(ISNULL(ss.QuantiteBase, 0)), 0) AS QuantitePieces,
                   ISNULL(SUM(ISNULL(ss.QuantiteBase, 0) * ISNULL(cp.CoutPiece, 0)), 0) AS MontantTotal
            FROM StockSortie ss
            LEFT JOIN MotifSortie m ON m.MotifId = ss.MotifId
            LEFT JOIN CoutPieceProduit cp ON cp.ProduitId = ss.ProduitId
            WHERE ss.DateSortie >= @DateDebut AND ss.DateSortie < DATEADD(DAY, 1, @DateFin)
              AND UPPER(ISNULL(ss.Source, '')) IN ('SORTIE_MANUELLE', 'MANUEL')
              AND (UPPER(ISNULL(m.Nature, '')) LIKE '%DETTE%' OR UPPER(ISNULL(m.Libelle, '')) LIKE '%DETTE%')
              AND (UPPER(ISNULL(m.Libelle, '')) LIKE '%CLIENT%' OR (UPPER(ISNULL(ss.StatutPaiement, '')) = 'IMPAYE' AND ss.ClientId IS NOT NULL))
            """;

        var list = new List<AnalyseVenteDetailRow>();

        await using (var cmd = new SqlCommand(sqlDepenses, cn))
        {
            cmd.Parameters.AddWithValue("@DateDebut", dateDebut.Date);
            cmd.Parameters.AddWithValue("@DateFin", dateFin.Date);
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                list.Add(new AnalyseVenteDetailRow
                {
                    Ordre = 10,
                    Rubrique = "Dépenses",
                    Categorie = reader.GetValue(0).ToString() ?? string.Empty,
                    QuantitePieces = 0m,
                    Montant = reader.IsDBNull(2) ? 0m : Convert.ToDecimal(reader.GetValue(2)),
                    Commentaire = reader.GetValue(1).ToString() ?? string.Empty + " dépense(s)"
                });
            }
        }

        await using (var cmd = new SqlCommand(sqlCharges, cn))
        {
            cmd.Parameters.AddWithValue("@DateDebut", dateDebut.Date);
            cmd.Parameters.AddWithValue("@DateFin", dateFin.Date);
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                list.Add(new AnalyseVenteDetailRow
                {
                    Ordre = 20,
                    Rubrique = "Charges",
                    Categorie = reader.GetValue(0).ToString() ?? string.Empty,
                    QuantitePieces = reader.IsDBNull(1) ? 0m : Convert.ToDecimal(reader.GetValue(1)),
                    Montant = reader.IsDBNull(2) ? 0m : Convert.ToDecimal(reader.GetValue(2)),
                    Commentaire = "Charge consommant du stock ou sans recette"
                });
            }
        }

        await using (var cmd = new SqlCommand(sqlCreances, cn))
        {
            cmd.Parameters.AddWithValue("@DateDebut", dateDebut.Date);
            cmd.Parameters.AddWithValue("@DateFin", dateFin.Date);
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                list.Add(new AnalyseVenteDetailRow
                {
                    Ordre = 15,
                    Rubrique = "Créances",
                    Categorie = reader.GetValue(0).ToString() ?? string.Empty,
                    QuantitePieces = reader.IsDBNull(1) ? 0m : Convert.ToDecimal(reader.GetValue(1)),
                    Montant = reader.IsDBNull(2) ? 0m : Convert.ToDecimal(reader.GetValue(2)),
                    Commentaire = "Vente à crédit non déduite du bénéfice net"
                });
            }
        }

        list.Sort((a, b) => a.Ordre.CompareTo(b.Ordre));
        return list;
    }

    private static decimal ReadDecimal(SqlDataReader reader, int ordinal)
        => reader.IsDBNull(ordinal) ? 0m : Convert.ToDecimal(reader.GetValue(ordinal));

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
