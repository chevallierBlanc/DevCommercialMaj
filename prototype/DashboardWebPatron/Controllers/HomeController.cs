using Microsoft.AspNetCore.Mvc;

namespace DashboardWebPatron.Controllers;

public sealed class HomeController : Controller
{
    public IActionResult Index() => View();

    public IActionResult Error()
    {
        Response.StatusCode = 500;
        return View();
    }
}
