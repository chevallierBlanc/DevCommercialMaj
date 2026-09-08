namespace DashboardWebPatron.Models;

public sealed class AnalyseVentePageViewModel
{
    public string Periode { get; set; } = "mois";
    public int Year { get; set; } = DateTime.Today.Year;
    public int Month { get; set; } = DateTime.Today.Month;
    public DateTime Date { get; set; } = DateTime.Today;
    public DateTime DateDebut { get; set; } = DateTime.Today.AddMonths(-1);
    public DateTime DateFin { get; set; } = DateTime.Today;
    public string NomBoutique { get; set; } = "ERP COMMERCIAL";
    public EntrepriseConfigurationDto Entreprise { get; set; } = new();
    public AnalyseVenteResponseDto? Analyse { get; set; }
}
