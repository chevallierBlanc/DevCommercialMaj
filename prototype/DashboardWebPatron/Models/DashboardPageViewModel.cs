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
        if (Periode.Equals("mois", StringComparison.OrdinalIgnoreCase) && Mensuel is not null)
        {
            return new[]
            {
                new DashboardMetricDto { Label = "CA du mois", Value = Mensuel.CaMensuel, Unit = "FC" },
                new DashboardMetricDto { Label = "Dépenses", Value = Mensuel.DepensesMensuelles, Unit = "FC" },
                new DashboardMetricDto { Label = "Bénéfice", Value = Mensuel.BeneficeEstime, Unit = "FC" },
                new DashboardMetricDto { Label = "Stock / sorties", Value = Mensuel.TotalSortiesManuelles, Unit = "pcs" }
            };
        }

        if (Periode.Equals("annee", StringComparison.OrdinalIgnoreCase) && Annuel is not null)
        {
            return new[]
            {
                new DashboardMetricDto { Label = "CA annuel", Value = Annuel.CaAnnuel, Unit = "FC" },
                new DashboardMetricDto { Label = "Dépenses", Value = Annuel.DepensesAnnuelles, Unit = "FC" },
                new DashboardMetricDto { Label = "Bénéfice", Value = Annuel.BeneficeEstime, Unit = "FC" },
                new DashboardMetricDto { Label = "Mouvements", Value = Annuel.TotalSortiesManuelles, Unit = "pcs" }
            };
        }

        if (Journalier is not null)
        {
            return new[]
            {
                new DashboardMetricDto { Label = "CA du jour", Value = Journalier.CaDuJour, Unit = "FC" },
                new DashboardMetricDto { Label = "Dépenses", Value = Journalier.DepensesDuJour, Unit = "FC" },
                new DashboardMetricDto { Label = "Bénéfice", Value = Journalier.BeneficeEstime, Unit = "FC" },
                new DashboardMetricDto { Label = "Sorties", Value = Journalier.TotalSorties, Unit = "pcs" }
            };
        }

        return Array.Empty<DashboardMetricDto>();
    }
}
