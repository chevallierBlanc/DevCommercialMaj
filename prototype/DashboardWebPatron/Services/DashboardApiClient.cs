using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using DashboardWebPatron.Models;
using Microsoft.Extensions.Options;

namespace DashboardWebPatron.Services;

public sealed class DashboardApiClient
{
    private readonly HttpClient _http;
    private readonly DashboardApiOptions _options;
    private string? _cachedAccessToken;
    private DateTime _cachedAccessTokenExpiresAtUtc;
    private readonly SemaphoreSlim _authLock = new(1, 1);

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
        await EnsureAuthenticatedAsync(ct);
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

    public async Task<AnalyseVentePageViewModel> LoadAnalyseAsync(string? periode, int? year, int? month, DateTime? date, CancellationToken ct = default)
    {
        await EnsureAuthenticatedAsync(ct);

        var mode = (periode ?? "mois").Trim().ToLowerInvariant();
        var today = date ?? DateTime.Today;
        var selectedYear = year ?? today.Year;
        var selectedMonth = month ?? today.Month;

        var model = new AnalyseVentePageViewModel
        {
            Periode = mode,
            Year = selectedYear,
            Month = selectedMonth,
            Date = today,
            DateDebut = mode == "annee"
                ? new DateTime(selectedYear, 1, 1)
                : new DateTime(selectedYear, selectedMonth, 1),
            DateFin = mode == "annee"
                ? new DateTime(selectedYear, 12, 31)
                : new DateTime(selectedYear, selectedMonth, DateTime.DaysInMonth(selectedYear, selectedMonth))
        };

        var query = $"api/dashboard/analyse-vente?periode={Uri.EscapeDataString(mode)}&year={selectedYear}&month={selectedMonth}&date={Uri.EscapeDataString(today.ToString("yyyy-MM-dd"))}";
        model.Analyse = await GetOrDefaultAsync<AnalyseVenteResponseDto>(query, ct);
        return model;
    }

    private async Task<T?> GetOrDefaultAsync<T>(string path, CancellationToken ct) where T : class
    {
        try
        {
            await EnsureAuthenticatedAsync(ct);
            return await _http.GetFromJsonAsync<T>(path, ct);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
        {
            if (await TryLoginAsync(ct))
            {
                return await _http.GetFromJsonAsync<T>(path, ct);
            }
            return null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
            return null;
        }
    }

    private async Task<bool> EnsureAuthenticatedAsync(CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(_options.AccessToken))
        {
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _options.AccessToken);
            return true;
        }

        if (!string.IsNullOrWhiteSpace(_cachedAccessToken) && _cachedAccessTokenExpiresAtUtc > DateTime.UtcNow.AddMinutes(2))
        {
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _cachedAccessToken);
            return true;
        }

        return await TryLoginAsync(ct);
    }

    private async Task<bool> TryLoginAsync(CancellationToken ct)
    {
        await _authLock.WaitAsync(ct);
        try
        {
            if (!string.IsNullOrWhiteSpace(_options.AccessToken))
            {
                _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _options.AccessToken);
                return true;
            }

            if (!string.IsNullOrWhiteSpace(_cachedAccessToken) && _cachedAccessTokenExpiresAtUtc > DateTime.UtcNow.AddMinutes(2))
            {
                _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _cachedAccessToken);
                return true;
            }

            var login = new LoginRequestDto
            {
                Username = _options.Username,
                Password = _options.Password
            };

            using var response = await _http.PostAsJsonAsync("api/auth/login", login, ct);
            if (!response.IsSuccessStatusCode)
            {
                return false;
            }

            var token = await response.Content.ReadFromJsonAsync<TokenResponseDto>(cancellationToken: ct);
            if (token is null || string.IsNullOrWhiteSpace(token.AccessToken))
            {
                return false;
            }

            _cachedAccessToken = token.AccessToken;
            _cachedAccessTokenExpiresAtUtc = token.AccessTokenExpiresAtUtc;
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _cachedAccessToken);
            return true;
        }
        finally
        {
            _authLock.Release();
        }
    }
}
