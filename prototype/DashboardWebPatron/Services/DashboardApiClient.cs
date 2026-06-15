using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Diagnostics;
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
        model.Analyse = await GetAnalyseAsync(query, ct)
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

    private async Task<AnalyseVenteResponseDto?> GetAnalyseAsync(string path, CancellationToken ct)
    {
        try
        {
            await EnsureAuthenticatedAsync(ct);
            return await GetAnalyseResponseAsync(path, ct);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
        {
            if (await TryLoginAsync(ct))
            {
                return await GetAnalyseResponseAsync(path, ct);
            }
            return null;
        }
        catch (Exception ex)
        {
            TraceAnalyse($"ANALYSE_ERROR path={path} error={ex.GetType().Name}: {ex.Message}");
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

    private async Task<AnalyseVenteResponseDto?> GetAnalyseResponseAsync(string path, CancellationToken ct)
    {
        using var response = await _http.GetAsync(path, ct);
        var raw = await response.Content.ReadAsStringAsync(ct);
        TraceAnalyse($"ANALYSE_HTTP path={path} status={(int)response.StatusCode} raw={raw}");

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new HttpRequestException("Unauthorized", null, response.StatusCode);
        }

        response.EnsureSuccessStatusCode();

        if (string.IsNullOrWhiteSpace(raw))
        {
            TraceAnalyse("ANALYSE_EMPTY_RESPONSE");
            return null;
        }

        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            NumberHandling = JsonNumberHandling.AllowReadingFromString
        };

        try
        {
            var dto = JsonSerializer.Deserialize<AnalyseVenteResponseDto>(raw, options);
            if (dto is not null)
            {
                return dto;
            }
        }
        catch (Exception ex)
        {
            TraceAnalyse($"ANALYSE_DESERIALIZE_ERROR type={ex.GetType().Name} message={ex.Message}");
        }

        try
        {
            using var doc = JsonDocument.Parse(raw);
            var root = doc.RootElement;
            var fallback = new AnalyseVenteResponseDto
            {
                DateDebut = ReadDate(root, "DateDebut"),
                DateFin = ReadDate(root, "DateFin"),
                PeriodeLabel = ReadString(root, "PeriodeLabel"),
                ValeurStockEntree = ReadDecimal(root, "ValeurStockEntree"),
                CoutMarchandisesVendues = ReadDecimal(root, "CoutMarchandisesVendues"),
                ChiffreAffaires = ReadDecimal(root, "ChiffreAffaires"),
                BeneficeRealise = ReadDecimal(root, "BeneficeRealise"),
                DepensesTotal = ReadDecimal(root, "DepensesTotal"),
                ChargesSortiesManuelles = ReadDecimal(root, "ChargesSortiesManuelles"),
                BeneficeNetRealise = ReadDecimal(root, "BeneficeNetRealise"),
                CoutStockRestant = ReadDecimal(root, "CoutStockRestant"),
                ProjectionBeneficeRestant = ReadDecimal(root, "ProjectionBeneficeRestant"),
                MargeBeneficiairePourcentage = ReadDecimal(root, "MargeBeneficiairePourcentage"),
                Evaluation = ReadString(root, "Evaluation")
            };

            if (root.TryGetProperty("Details", out var detailsEl) && detailsEl.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in detailsEl.EnumerateArray())
                {
                    fallback.Details.Add(new AnalyseVenteDetailRowDto
                    {
                        Ordre = ReadInt(item, "Ordre"),
                        Rubrique = ReadString(item, "Rubrique"),
                        Categorie = ReadString(item, "Categorie"),
                        QuantitePieces = ReadDecimal(item, "QuantitePieces"),
                        Montant = ReadDecimal(item, "Montant"),
                        Commentaire = ReadString(item, "Commentaire")
                    });
                }
            }

            var hasKnownShape = root.ValueKind == JsonValueKind.Object
                && (root.TryGetProperty("ValeurStockEntree", out _)
                    || root.TryGetProperty("CoutMarchandisesVendues", out _)
                    || root.TryGetProperty("ChiffreAffaires", out _)
                    || root.TryGetProperty("BeneficeRealise", out _)
                    || root.TryGetProperty("DepensesTotal", out _)
                    || root.TryGetProperty("ChargesSortiesManuelles", out _)
                    || root.TryGetProperty("BeneficeNetRealise", out _)
                    || root.TryGetProperty("CoutStockRestant", out _)
                    || root.TryGetProperty("ProjectionBeneficeRestant", out _)
                    || root.TryGetProperty("MargeBeneficiairePourcentage", out _)
                    || root.TryGetProperty("Evaluation", out _)
                    || root.TryGetProperty("Details", out _));

            TraceAnalyse($"ANALYSE_FALLBACK_PARSED hasKnownShape={hasKnownShape} details={fallback.Details.Count}");
            return hasKnownShape ? fallback : null;
        }
        catch (Exception ex)
        {
            TraceAnalyse($"ANALYSE_FALLBACK_ERROR type={ex.GetType().Name} message={ex.Message}");
            return null;
        }
    }

    private async Task<AnalyseVenteResponseDto?> GetAnalyseFallbackAsync(string mode, int year, int month, DateTime today, CancellationToken ct)
    {
        var fallbackQuery = $"api/dashboard/analyse-vente?periode={Uri.EscapeDataString(mode)}&year={year}&month={month}&date={Uri.EscapeDataString(today.ToString("yyyy-MM-dd"))}";
        return await GetAnalyseAsync(fallbackQuery, ct);
    }

    [Conditional("DEBUG")]
    private static void TraceAnalyse(string message)
    {
        Debug.WriteLine(message);
    }

    private static string ReadString(JsonElement root, string name)
        => root.TryGetProperty(name, out var value) ? value.ToString() ?? string.Empty : string.Empty;

    private static int ReadInt(JsonElement root, string name)
        => root.TryGetProperty(name, out var value) && value.TryGetInt32(out var n) ? n : 0;

    private static decimal ReadDecimal(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var value))
        {
            return 0m;
        }

        if (value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out var number))
        {
            return number;
        }

        if (value.ValueKind == JsonValueKind.String && decimal.TryParse(value.GetString(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var parsed))
        {
            return parsed;
        }

        return 0m;
    }

    private static DateTime ReadDate(JsonElement root, string name)
        => root.TryGetProperty(name, out var value) && DateTime.TryParse(value.ToString(), out var parsed) ? parsed : default;

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
