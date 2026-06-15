using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
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
        var (startDate, endDate) = ResolvePeriodRange(mode, selectedYear, selectedMonth, today);

        var model = new DashboardPageViewModel
        {
            Periode = mode,
            Year = selectedYear,
            Month = selectedMonth,
            Date = today
        };

        var query = $"api/dashboard/journalier?start={Uri.EscapeDataString(startDate.ToString("yyyy-MM-dd"))}&end={Uri.EscapeDataString(endDate.ToString("yyyy-MM-dd"))}&date={Uri.EscapeDataString(today.ToString("yyyy-MM-dd"))}";
        model.Journalier = await GetOrDefaultAsync<JournalierDashboardResponseDto>(query, ct);

        return model;
    }

    public async Task<AnalyseVentePageViewModel> LoadAnalyseAsync(string? periode, int? year, int? month, DateTime? date, CancellationToken ct = default)
    {
        await EnsureAuthenticatedAsync(ct);

        var mode = (periode ?? "mois").Trim().ToLowerInvariant();
        var today = date ?? DateTime.Today;
        var selectedYear = year ?? today.Year;
        var selectedMonth = month ?? today.Month;
        var (startDate, endDate) = ResolvePeriodRange(mode, selectedYear, selectedMonth, today);

        var model = new AnalyseVentePageViewModel
        {
            Periode = mode,
            Year = selectedYear,
            Month = selectedMonth,
            Date = today,
            DateDebut = startDate,
            DateFin = endDate
        };

        var query = $"api/dashboard/analyse-vente?periode={Uri.EscapeDataString(mode)}&year={selectedYear}&month={selectedMonth}&date={Uri.EscapeDataString(today.ToString("yyyy-MM-dd"))}&start={Uri.EscapeDataString(startDate.ToString("yyyy-MM-dd"))}&end={Uri.EscapeDataString(endDate.ToString("yyyy-MM-dd"))}";
        model.Analyse = await GetOrDefaultAsync<AnalyseVenteResponseDto>(query, ct)
            ?? await GetAnalyseFallbackAsync(mode, selectedYear, selectedMonth, today, ct);
        return model;
    }

    private static (DateTime Start, DateTime End) ResolvePeriodRange(string mode, int year, int month, DateTime referenceDate)
    {
        if (mode == "annee")
        {
            return (new DateTime(year, 1, 1), new DateTime(year, 12, 31));
        }

        if (mode == "semaine")
        {
            var offset = ((int)referenceDate.DayOfWeek + 6) % 7;
            var start = referenceDate.Date.AddDays(-offset);
            return (start, start.AddDays(6));
        }

        if (mode == "jour")
        {
            var start = referenceDate.Date;
            return (start, start);
        }

        var monthStart = new DateTime(year, month, 1);
        return (monthStart, monthStart.AddMonths(1).AddDays(-1));
    }

    private async Task<T?> GetOrDefaultAsync<T>(string path, CancellationToken ct) where T : class
    {
        try
        {
            await EnsureAuthenticatedAsync(ct);
            return await GetJsonAsync<T>(path, ct);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
        {
            if (await TryLoginAsync(ct))
            {
                return await GetJsonAsync<T>(path, ct);
            }
            return null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
            return null;
        }
    }

    private async Task<T?> GetJsonAsync<T>(string path, CancellationToken ct) where T : class
    {
        using var response = await _http.GetAsync(path, ct);
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new HttpRequestException("Unauthorized", null, response.StatusCode);
        }

        response.EnsureSuccessStatusCode();

        var raw = await response.Content.ReadAsStringAsync(ct);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            NumberHandling = JsonNumberHandling.AllowReadingFromString
        };
        return JsonSerializer.Deserialize<T>(raw, options);
    }

    private async Task<AnalyseVenteResponseDto?> GetAnalyseFallbackAsync(string mode, int year, int month, DateTime today, CancellationToken ct)
    {
        var fallbackQuery = $"api/dashboard/analyse-vente?periode={Uri.EscapeDataString(mode)}&year={year}&month={month}&date={Uri.EscapeDataString(today.ToString("yyyy-MM-dd"))}";
        return await GetOrDefaultAsync<AnalyseVenteResponseDto>(fallbackQuery, ct);
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
