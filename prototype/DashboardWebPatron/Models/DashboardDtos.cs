namespace DashboardWebPatron.Models;

public sealed class DashboardMetricDto
{
    public string Label { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public string Unit { get; set; } = string.Empty;
}

public sealed class DailyProductRowDto
{
    public string Product { get; set; } = string.Empty;
    public decimal QuantitySold { get; set; }
    public string TypeVente { get; set; } = string.Empty;
    public decimal AmountGenerated { get; set; }
    public DateTime Hour { get; set; }
    public string Agent { get; set; } = string.Empty;
}

public sealed class ManualExitRowDto
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

public sealed class ExpenseRowDto
{
    public string Category { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

public sealed class StockAlertRowDto
{
    public string Product { get; set; } = string.Empty;
    public decimal Stock { get; set; }
}

public sealed class PeriodSeriesPointDto
{
    public string Label { get; set; } = string.Empty;
    public decimal Value { get; set; }
}

public sealed class JournalierDashboardResponseDto
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
    public List<DailyProductRowDto> ProduitsVendus { get; set; } = [];
    public List<ManualExitRowDto> SortiesManuelles { get; set; } = [];
    public List<ExpenseRowDto> DepensesParCategorie { get; set; } = [];
    public List<StockAlertRowDto> AlertesStockFaible { get; set; } = [];
    public List<PeriodSeriesPointDto> SeriesVentes { get; set; } = [];
    public List<PeriodSeriesPointDto> SeriesDepenses { get; set; } = [];
}

public sealed class MensuelDashboardResponseDto
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
    public List<DailyProductRowDto> TopProduits { get; set; } = [];
    public List<ExpenseRowDto> TopDepenses { get; set; } = [];
    public List<PeriodSeriesPointDto> EvolutionVentes { get; set; } = [];
    public List<PeriodSeriesPointDto> EvolutionSorties { get; set; } = [];
    public List<PeriodSeriesPointDto> EvolutionDepenses { get; set; } = [];
}

public sealed class AnnuelDashboardResponseDto
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
    public List<PeriodSeriesPointDto> VentesParMois { get; set; } = [];
    public List<PeriodSeriesPointDto> DepensesParMois { get; set; } = [];
    public List<PeriodSeriesPointDto> BeneficesParMois { get; set; } = [];
    public List<ExpenseRowDto> CategoriesDepensesGourmandes { get; set; } = [];
    public List<DailyProductRowDto> TopProduits { get; set; } = [];
}
