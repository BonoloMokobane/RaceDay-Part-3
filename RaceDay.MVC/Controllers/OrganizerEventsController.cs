using Microsoft.AspNetCore.Mvc;
using RaceDay.MVC.Models;
using RaceDay.MVC.Services;

namespace RaceDay.MVC.Controllers;

public class OrganizerEventsController : Controller
{
    private readonly RaceDayApiService _api;

    public OrganizerEventsController(RaceDayApiService api)
    {
        _api = api;
    }

    private string? Token => HttpContext.Session.GetString("ApiToken");
    private string? Cookie => HttpContext.Session.GetString("ApiCookie");

    private bool IsOrganizer =>
        string.Equals(
            HttpContext.Session.GetString("UserRole"),
            "Organizer",
            StringComparison.OrdinalIgnoreCase);

    public async Task<IActionResult> Index()
    {
        if (!IsOrganizer)
            return RedirectToAction("Login", "Account");

        var events = await _api.GetEventsAsync(Token, Cookie);
        return View(events);
    }

    [HttpGet]
    public IActionResult Create()
    {
        if (!IsOrganizer)
            return RedirectToAction("Login", "Account");

        return View(new EventViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EventViewModel model)
    {
        if (!IsOrganizer)
            return RedirectToAction("Login", "Account");

        if (!ModelState.IsValid)
            return View(model);

        var result = await _api.CreateEventAsync(model, Token, Cookie);

        if (!result.Success)
        {
            ModelState.AddModelError("", result.Message);
            return View(model);
        }

        TempData["Success"] = "Your event was created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        if (!IsOrganizer)
            return RedirectToAction("Login", "Account");

        var model = await _api.GetEventAsync(id, Token, Cookie);

        if (model == null)
            return NotFound();

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EventViewModel model)
    {
        if (!IsOrganizer)
            return RedirectToAction("Login", "Account");

        if (!ModelState.IsValid)
            return View(model);

        var result = await _api.UpdateEventAsync(model, Token, Cookie);

        if (!result.Success)
        {
            ModelState.AddModelError("", result.Message);
            return View(model);
        }

        TempData["Success"] = "Your event was updated successfully.";
        return RedirectToAction(nameof(Index));
    }
}
