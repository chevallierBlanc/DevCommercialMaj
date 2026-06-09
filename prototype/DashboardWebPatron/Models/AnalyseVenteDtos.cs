namespace DashboardWebPatron.Models;

public sealed class AnalyseVenteDetailRowDto
{
    public int Ordre { get; set; }
    public string Rubrique { get; set; } = string.Empty;
    public string Categorie { get; set; } = string.Empty;
    public decimal QuantitePieces { get; set; }
    public decimal Montant { get; set; }
    public string Commentaire { get; set; } = string.Empty;
}

public sealed class AnalyseVenteResponseDto
{
    public DateTime DateDebut { get; set; }
    public DateTime DateFin { get; set; }
    public string PeriodeLabel { get; set; } = string.Empty;
    public decimal ValeurStockEntree { get; set; }
    public decimal CoutMarchandisesVendues { get; set; }
    public decimal ChiffreAffaires { get; set; }
    public decimal BeneficeRealise { get; set; }
    public decimal DepensesTotal { get; set; }
    public decimal ChargesSortiesManuelles { get; set; }
    public decimal BeneficeNetRealise { get; set; }
    public decimal CoutStockRestant { get; set; }
    public decimal ProjectionBeneficeRestant { get; set; }
    public decimal MargeBeneficiairePourcentage { get; set; }
    public string Evaluation { get; set; } = string.Empty;
    public List<AnalyseVenteDetailRowDto> Details { get; set; } = [];
}
