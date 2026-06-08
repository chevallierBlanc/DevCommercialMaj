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
                new DashboardMetricDto { Label = "CA du mois", Value = Mensuel.CaMensuel },
                new DashboardMetricDto { Label = "Dépenses", Value = Mensuel.DepensesMensuelles },
                new DashboardMetricDto { Label = "Bénéfice", Value = Mensuel.BeneficeEstime },
                new DashboardMetricDto { Label = "Stock / sorties", Value = Mensuel.TotalSortiesManuelles }
            };
        }

        if (Periode.Equals("annee", StringComparison.OrdinalIgnoreCase) && Annuel is not null)
        {
            return new[]
            {
                new DashboardMetricDto { Label = "CA annuel", Value = Annuel.CaAnnuel },
                new DashboardMetricDto { Label = "Dépenses", Value = Annuel.DepensesAnnuelles },
                new DashboardMetricDto { Label = "Bénéfice", Value = Annuel.BeneficeEstime },
                new DashboardMetricDto { Label = "Mouvements", Value = Annuel.TotalSortiesManuelles }
            };
        }

        if (Journalier is not null)
        {
            return new[]
            {
                new DashboardMetricDto { Label = "CA du jour", Value = Journalier.CaDuJour },
                new DashboardMetricDto { Label = "Dépenses", Value = Journalier.DepensesDuJour },
                new DashboardMetricDto { Label = "Bénéfice", Value = Journalier.BeneficeEstime },
                new DashboardMetricDto { Label = "Sorties", Value = Journalier.TotalSorties }
            };
        }

        return Array.Empty<DashboardMetricDto>();
    }
}
