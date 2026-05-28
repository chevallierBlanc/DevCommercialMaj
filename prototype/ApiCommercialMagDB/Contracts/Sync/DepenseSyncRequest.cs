namespace CommercialMagDb.Api.Contracts.Sync;

public sealed class DepenseSyncRequest
{
    public int? Id { get; set; }
    public string Categorie { get; set; } = string.Empty;
    public decimal Montant { get; set; }
    public string Devise { get; set; } = "FC";
    public string? Description { get; set; }
    public DateTime? DateDepense { get; set; }
    public string Source { get; set; } = "API";
    public string TypeDepense { get; set; } = string.Empty;
    public string? CreePar { get; set; }
}
