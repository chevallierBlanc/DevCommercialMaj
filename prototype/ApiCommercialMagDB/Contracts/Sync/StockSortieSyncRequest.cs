namespace CommercialMagDb.Api.Contracts.Sync;

public sealed class StockSortieSyncRequest
{
    public string NumeroSortie { get; set; } = string.Empty;
    public int? ClientId { get; set; }
    public int? MotifId { get; set; }
    public string? Source { get; set; }
    public int? CreePar { get; set; }
    public DateTime? DateSortie { get; set; }
    public string? Observation { get; set; }
    public List<StockSortieSyncLine> Lignes { get; set; } = [];
}

public sealed class StockSortieSyncLine
{
    public int ProduitId { get; set; }
    public decimal QuantiteSaisie { get; set; }
    public string? Unite { get; set; }
    public decimal QuantiteBase { get; set; }
    public string? TypeVente { get; set; }
    public decimal? PrixUnitaire { get; set; }
    public decimal? MontantLigne { get; set; }
    public string? StatutPaiement { get; set; }
    public decimal? MontantPaye { get; set; }
    public decimal? ResteAPayer { get; set; }
    public string? Observation { get; set; }
}
