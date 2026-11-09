using Microsoft.AspNetCore.Mvc;
using RaceDay.MVC.Models;
using RaceDay.MVC.Services;

namespace RaceDay.MVC.Controllers;

public class ParticipantController : Controller
{
    private readonly RaceDayApiService _api;

    public ParticipantController(RaceDayApiService api)
    {
        _api = api;
    }

    public IActionResult Index()
    {
        var role = HttpContext.Session.GetString("UserRole");

        if (role != "Participant")
            return RedirectToAction("Login", "Account");

        ViewBag.Email = HttpContext.Session.GetString("UserEmail");

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadProfileImage(
        ImageUploadViewModel model)
    {
        var role = HttpContext.Session.GetString("UserRole");

        if (role != "Participant")
            return RedirectToAction("Login", "Account");

        if (model.Image == null || model.Image.Length == 0)
        {
            TempData["Error"] = "Please select an image.";
            return RedirectToAction("Index");
        }

        var token = HttpContext.Session.GetString("ApiToken");
        var cookie = HttpContext.Session.GetString("ApiCookie");

        var result = await _api.UploadProfileImageAsync(
            model.Image, token, cookie);

        TempData[result.Success ? "Success" : "Error"] = result.Message;

        return RedirectToAction("Index");
    }
}
