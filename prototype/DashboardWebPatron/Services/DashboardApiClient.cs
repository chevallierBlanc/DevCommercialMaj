using System.Net.Http.Headers;
using System.Net.Http.Json;
using DashboardWebPatron.Models;
using Microsoft.Extensions.Options;

namespace DashboardWebPatron.Services;

public sealed class DashboardApiClient
{
    private readonly HttpClient _http;
    private readonly DashboardApiOptions _options;

    public DashboardApiClient(HttpClient http, IOptions<DashboardApiOptions> options)
    {
        _http = http;
        _options = options.Value;
        _http.BaseAddress = new Uri(_options.BaseUrl, UriKind.Absolute);
        if (!string.IsNullOrWhiteSpace(_options.AccessToken))
        {
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _options.AccessToken);
        }
    }

    public async Task<DashboardPageViewModel> LoadAsync(string? periode, int? year, int? month, DateTime? date, CancellationToken ct = default)
    {
        var mode = (periode ?? "jour").Trim().ToLowerInvariant();
        var today = date ?? DateTime.Today;
        var selectedYear = year ?? today.Year;
        var selectedMonth = month ?? today.Month;

        var model = new DashboardPageViewModel
        {
            Periode = mode,
            Year = selectedYear,
            Month = selectedMonth,
            Date = today
        };

        model.Journalier = await GetOrDefaultAsync<JournalierDashboardResponseDto>($"api/dashboard/journalier?date={Uri.EscapeDataString(today.ToString("yyyy-MM-dd"))}", ct);
        model.Mensuel = await GetOrDefaultAsync<MensuelDashboardResponseDto>($"api/dashboard/mensuel?year={selectedYear}&month={selectedMonth}", ct);
        model.Annuel = await GetOrDefaultAsync<AnnuelDashboardResponseDto>($"api/dashboard/annuel?year={selectedYear}", ct);

        return model;
    }

    private async Task<T?> GetOrDefaultAsync<T>(string path, CancellationToken ct) where T : class
    {
        try
        {
            return await _http.GetFromJsonAsync<T>(path, ct);
        }
        catch
        {
            return null;
        }
    }
}
