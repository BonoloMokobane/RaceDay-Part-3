
using Microsoft.AspNetCore.Mvc;
using RaceDay.MVC.Services;

namespace RaceDay.MVC.Controllers;

public class EnrollmentController : Controller
{
    private readonly RaceDayApiService _api;

    public EnrollmentController(RaceDayApiService api)
    {
        _api = api;
    }

    private string? Token => HttpContext.Session.GetString("ApiToken");
    private string? Cookie => HttpContext.Session.GetString("ApiCookie");

    private bool IsParticipant =>
        HttpContext.Session.GetString("UserRole") == "Participant";

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Enroll(int eventId, int categoryId)
    {
        if (!IsParticipant)
            return RedirectToAction("Login", "Account");

        var result = await _api.EnrollAsync(
            eventId, categoryId, Token, Cookie);

        TempData["Success"] = result.Success
            ? "You have successfully enrolled."
            : result.Message;

        return RedirectToAction("Details", "BrowseEvents",
            new { id = eventId });
    }

    public async Task<IActionResult> My()
    {
        if (!IsParticipant)
            return RedirectToAction("Login", "Account");

        var enrollments = await _api.GetMyEnrollmentsAsync(
            Token, Cookie);

        return View(enrollments);
    }

    public async Task<IActionResult> Event(int eventId)
    {
        if (HttpContext.Session.GetString("UserRole") != "Organizer")
            return RedirectToAction("Login", "Account");

        var enrollments = await _api.GetEventEnrollmentsAsync(
            eventId, Token, Cookie);

        ViewBag.EventId = eventId;
        return View(enrollments);
    }
}
