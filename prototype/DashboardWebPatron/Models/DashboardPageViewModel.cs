namespace DashboardWebPatron.Models;

public sealed class DashboardPageViewModel
{
    public string Periode { get; set; } = "jour";
    public int Year { get; set; } = DateTime.Today.Year;
    public int Month { get; set; } = DateTime.Today.Month;
    public DateTime Date { get; set; } = DateTime.Today;

    public JournalierDashboardResponseDto? Journalier { get; set; }
    public MensuelDashboardResponseDto? Mensuel { get; set; }
    public AnnuelDashboardResponseDto? Annuel { get; set; }

    public IEnumerable<DashboardMetricDto> Metrics => BuildMetrics();

    private IEnumerable<DashboardMetricDto> BuildMetrics()
    {
        if (Journalier is null)
        {
            return Array.Empty<DashboardMetricDto>();
        }

        var isJour = Periode.Equals("jour", StringComparison.OrdinalIgnoreCase);
        var caLabel = isJour ? "CA du jour" : Periode.Equals("semaine", StringComparison.OrdinalIgnoreCase) ? "CA de la semaine" : Periode.Equals("annee", StringComparison.OrdinalIgnoreCase) ? "CA annuel" : "CA de la période";
        var mouvementLabel = isJour ? "Sorties" : "Mouvements";

        return new[]
        {
            new DashboardMetricDto { Label = caLabel, Value = Journalier.CaDuJour, Unit = "FC" },
            new DashboardMetricDto { Label = "Dépenses", Value = Journalier.DepensesDuJour, Unit = "FC" },
            new DashboardMetricDto { Label = "Bénéfice", Value = Journalier.BeneficeEstime, Unit = "FC" },
            new DashboardMetricDto { Label = mouvementLabel, Value = Journalier.TotalSorties, Unit = "pcs" }
        };
    }
}
