using Microsoft.AspNetCore.Mvc;

namespace RaceDay.MVC.Controllers;

public class OrganizerController : Controller
{
    public IActionResult Index()
    {
        var role = HttpContext.Session.GetString("UserRole");

        if (role != "Organizer")
            return RedirectToAction("Login", "Account");

        ViewBag.Email = HttpContext.Session.GetString("UserEmail");

        return View();
    }
}