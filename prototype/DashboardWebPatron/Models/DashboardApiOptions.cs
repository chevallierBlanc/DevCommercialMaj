namespace DashboardWebPatron.Models;

public sealed class DashboardApiOptions
{
    public string BaseUrl { get; set; } = "http://localhost:5080/";
    public string? AccessToken { get; set; }
}
