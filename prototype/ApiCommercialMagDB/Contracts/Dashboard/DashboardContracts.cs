namespace CommercialMagDb.Api.Contracts.Dashboard;

public sealed class DashboardMetric
{
    public string Label { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public string Unit { get; set; } = string.Empty;
}

public sealed class DailyProductRow
{
    public string Product { get; set; } = string.Empty;
    public decimal QuantitySold { get; set; }
    public string TypeVente { get; set; } = string.Empty;
    public decimal AmountGenerated { get; set; }
    public DateTime Hour { get; set; }
    public string Agent { get; set; } = string.Empty;
}

public sealed class ManualExitRow
{
    public string Product { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string Motif { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string User { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public decimal Amount { get; set; }
    public string Observation { get; set; } = string.Empty;
}

public sealed class ExpenseRow
{
    public string Category { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

public sealed class StockAlertRow
{
    public string Product { get; set; } = string.Empty;
    public decimal Stock { get; set; }
}

public sealed class PeriodSeriesPoint
{
    public string Label { get; set; } = string.Empty;
    public decimal Value { get; set; }
}

public sealed class JournalierDashboardResponse
{
    public DateTime Date { get; set; }
    public decimal CaDuJour { get; set; }
    public decimal TotalSorties { get; set; }
    public decimal DepensesDuJour { get; set; }
    public decimal BeneficeEstime { get; set; }
    public decimal TotalEntrees { get; set; }
    public decimal TotalVentes { get; set; }
    public decimal TotalSortiesManuelles { get; set; }
    public decimal TotalPertes { get; set; }
    public decimal TotalDons { get; set; }
    public decimal TotalAllocations { get; set; }
    public decimal TotalDettesClients { get; set; }
    public decimal TotalDettesBoss { get; set; }
    public decimal TotalSortiesHorsCaisse { get; set; }
    public decimal TotalGros { get; set; }
    public decimal TotalDemi { get; set; }
    public decimal TotalQuart { get; set; }
    public decimal TotalPiece { get; set; }
    public decimal TotalDouzaine { get; set; }
    public decimal MontantTotalGenere { get; set; }
    public List<DailyProductRow> ProduitsVendus { get; set; } = [];
    public List<ManualExitRow> SortiesManuelles { get; set; } = [];
    public List<ExpenseRow> DepensesParCategorie { get; set; } = [];
    public List<StockAlertRow> AlertesStockFaible { get; set; } = [];
    public List<PeriodSeriesPoint> SeriesVentes { get; set; } = [];
    public List<PeriodSeriesPoint> SeriesDepenses { get; set; } = [];
}

public sealed class MensuelDashboardResponse
{
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal CaMensuel { get; set; }
    public decimal DepensesMensuelles { get; set; }
    public decimal BeneficeEstime { get; set; }
    public decimal TotalEntrees { get; set; }
    public decimal TotalVentes { get; set; }
    public decimal TotalSortiesManuelles { get; set; }
    public decimal TotalPertes { get; set; }
    public decimal TotalDons { get; set; }
    public decimal TotalAllocations { get; set; }
    public decimal TotalDettesClients { get; set; }
    public decimal TotalDettesBoss { get; set; }
    public decimal TotalSortiesHorsCaisse { get; set; }
    public decimal TotalGros { get; set; }
    public decimal TotalDemi { get; set; }
    public decimal TotalQuart { get; set; }
    public decimal TotalPiece { get; set; }
    public decimal TotalDouzaine { get; set; }
    public decimal MontantTotalGenere { get; set; }
    public List<DailyProductRow> TopProduits { get; set; } = [];
    public List<ExpenseRow> TopDepenses { get; set; } = [];
    public List<PeriodSeriesPoint> EvolutionVentes { get; set; } = [];
    public List<PeriodSeriesPoint> EvolutionSorties { get; set; } = [];
    public List<PeriodSeriesPoint> EvolutionDepenses { get; set; } = [];
}

public sealed class AnnuelDashboardResponse
{
    public int Year { get; set; }
    public decimal CaAnnuel { get; set; }
    public decimal DepensesAnnuelles { get; set; }
    public decimal BeneficeEstime { get; set; }
    public decimal TotalEntrees { get; set; }
    public decimal TotalVentes { get; set; }
    public decimal TotalSortiesManuelles { get; set; }
    public decimal TotalPertes { get; set; }
    public decimal TotalDons { get; set; }
    public decimal TotalAllocations { get; set; }
    public decimal TotalDettesClients { get; set; }
    public decimal TotalDettesBoss { get; set; }
    public decimal TotalSortiesHorsCaisse { get; set; }
    public decimal TotalGros { get; set; }
    public decimal TotalDemi { get; set; }
    public decimal TotalQuart { get; set; }
    public decimal TotalPiece { get; set; }
    public decimal TotalDouzaine { get; set; }
    public decimal MontantTotalGenere { get; set; }
    public List<PeriodSeriesPoint> VentesParMois { get; set; } = [];
    public List<PeriodSeriesPoint> DepensesParMois { get; set; } = [];
    public List<PeriodSeriesPoint> BeneficesParMois { get; set; } = [];
    public List<ExpenseRow> CategoriesDepensesGourmandes { get; set; } = [];
    public List<DailyProductRow> TopProduits { get; set; } = [];
}
