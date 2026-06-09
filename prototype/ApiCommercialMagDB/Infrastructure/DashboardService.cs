using CommercialMagDb.Api.Contracts.Dashboard;
namespace CommercialMagDb.Api.Infrastructure;

public sealed class DashboardService(DashboardRepository repository)
{
    public Task<JournalierDashboardResponse> GetJournalierAsync(DateTime date, CancellationToken ct = default)
        => repository.GetJournalierAsync(date, ct);

    public Task<MensuelDashboardResponse> GetMensuelAsync(int year, int month, CancellationToken ct = default)
        => repository.GetMensuelAsync(year, month, ct);

    public Task<AnnuelDashboardResponse> GetAnnuelAsync(int year, CancellationToken ct = default)
        => repository.GetAnnuelAsync(year, ct);

    public Task<AnalyseVenteResponse> GetAnalyseVenteAsync(DateTime dateDebut, DateTime dateFin, CancellationToken ct = default)
        => repository.GetAnalyseVenteAsync(dateDebut, dateFin, ct);
}
