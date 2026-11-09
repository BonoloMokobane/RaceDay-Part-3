using Microsoft.AspNetCore.Mvc;
using RaceDay.MVC.Services;

namespace RaceDay.MVC.Controllers;

public class BrowseEventsController : Controller
{
    private readonly RaceDayApiService _api;

    public BrowseEventsController(RaceDayApiService api)
    {
        _api = api;
    }

    private string? Token => HttpContext.Session.GetString("ApiToken");
    private string? Cookie => HttpContext.Session.GetString("ApiCookie");

    private bool IsParticipant =>
        string.Equals(
            HttpContext.Session.GetString("UserRole"),
            "Participant",
            StringComparison.OrdinalIgnoreCase);

    public async Task<IActionResult> Index()
    {
        if (!IsParticipant)
            return RedirectToAction("Login", "Account");

        var events = await _api.GetEventsAsync(Token, Cookie);
        return View(events);
    }

    public async Task<IActionResult> Details(int id)
    {
        if (!IsParticipant)
            return RedirectToAction("Login", "Account");

        var model = await _api.GetEventAsync(id, Token, Cookie);

        if (model == null)
            return NotFound();

        return View(model);
    }
}
