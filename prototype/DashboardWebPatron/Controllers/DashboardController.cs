using DashboardWebPatron.Models;
using DashboardWebPatron.Services;
using Microsoft.AspNetCore.Mvc;

namespace DashboardWebPatron.Controllers;

public sealed class DashboardController : Controller
{
    private readonly DashboardApiClient _client;

    public DashboardController(DashboardApiClient client)
    {
        _client = client;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? periode = "jour", int? year = null, int? month = null, DateTime? date = null, CancellationToken ct = default)
    {
        var model = await _client.LoadAsync(periode, year, month, date, ct);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Tv(int? year = null, int? month = null, DateTime? date = null, CancellationToken ct = default)
    {
        var model = await _client.LoadAsync("jour", year, month, date, ct);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Mobile(int? year = null, int? month = null, DateTime? date = null, CancellationToken ct = default)
    {
        var model = await _client.LoadAsync("jour", year, month, date, ct);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> AnalyseVente(string? periode = "mois", int? year = null, int? month = null, DateTime? date = null, CancellationToken ct = default)
    {
        var model = await _client.LoadAnalyseAsync(periode, year, month, date, ct);
        return View(model);
    }
}
