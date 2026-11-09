
using Microsoft.AspNetCore.Mvc;
using RaceDay.MVC.Models;
using RaceDay.MVC.Services;

namespace RaceDay.MVC.Controllers;

public class ResultsController : Controller
{
    private readonly RaceDayApiService _api;

    public ResultsController(RaceDayApiService api)
    {
        _api = api;
    }

    private string? Token => HttpContext.Session.GetString("ApiToken");
    private string? Cookie => HttpContext.Session.GetString("ApiCookie");

    private bool IsOrganizer =>
        HttpContext.Session.GetString("UserRole") == "Organizer";

    private bool IsParticipant =>
        HttpContext.Session.GetString("UserRole") == "Participant";

    [HttpGet]
    public IActionResult Create(int enrollmentId)
    {
        if (!IsOrganizer)
            return RedirectToAction("Login", "Account");

        return View(new ResultViewModel
        {
            EnrollmentId = enrollmentId
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ResultViewModel model)
    {
        if (!IsOrganizer)
            return RedirectToAction("Login", "Account");

        if (!ModelState.IsValid)
            return View(model);

        var result = await _api.CreateResultAsync(
            model, Token, Cookie);

        if (!result.Success)
        {
            ModelState.AddModelError("", result.Message);
            return View(model);
        }

        TempData["Success"] = "Result captured successfully.";

        return RedirectToAction("MyEvent",
            new { eventId = model.EventId });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int enrollmentId)
    {
        if (!IsOrganizer)
            return RedirectToAction("Login", "Account");

        var results = await _api.GetEventResultsAsync(
            0, Token, Cookie);

        var model = results.FirstOrDefault(
            r => r.EnrollmentId == enrollmentId);

        if (model == null)
            return NotFound();

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ResultViewModel model)
    {
        if (!IsOrganizer)
            return RedirectToAction("Login", "Account");

        if (!ModelState.IsValid)
            return View(model);

        var result = await _api.UpdateResultAsync(
            model, Token, Cookie);

        if (!result.Success)
        {
            ModelState.AddModelError("", result.Message);
            return View(model);
        }

        TempData["Success"] = "Result updated successfully.";

        return RedirectToAction("MyEvent",
            new { eventId = model.EventId });
    }

    public async Task<IActionResult> My()
    {
        if (!IsParticipant)
            return RedirectToAction("Login", "Account");

        var results = await _api.GetMyResultsAsync(Token, Cookie);
        return View(results);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int enrollmentId, int eventId)
    {
        if (!IsOrganizer)
            return RedirectToAction("Login", "Account");

        var results = await _api.GetEventResultsAsync(
            eventId, Token, Cookie);

        var model = results.FirstOrDefault(
            r => r.EnrollmentId == enrollmentId);

        if (model == null)
            return NotFound();

        return View(model);
    }
}


    