using Microsoft.AspNetCore.Mvc;
using RaceDay.MVC.Models;
using RaceDay.MVC.Services;

namespace RaceDay.MVC.Controllers;

public class AccountController : Controller
{
    private readonly RaceDayApiService _api;

    public AccountController(RaceDayApiService api)
    {
        _api = api;
    }

    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var result = await _api.RegisterAsync(new
        {
            firstName = model.FirstName,
            lastName = model.LastName,
            email = model.Email,
            password = model.Password,
            phoneNumber = model.PhoneNumber,
            role = model.Role
        });

        if (!result.Success)
        {
            ModelState.AddModelError("", result.Message);
            return View(model);
        }

        TempData["Success"] = "Registration successful. Please log in.";
        return RedirectToAction("Login");
    }

    [HttpGet]
    public IActionResult Login()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var result = await _api.LoginAsync(
            model.Email, model.Password);

        if (!result.Success)
        {
            ModelState.AddModelError("", result.Message);
            return View(model);
        }

        HttpContext.Session.SetString("UserEmail", model.Email);
        HttpContext.Session.SetString("UserRole", result.Role!);

        if (!string.IsNullOrWhiteSpace(result.Token))
            HttpContext.Session.SetString("ApiToken", result.Token);

        if (!string.IsNullOrWhiteSpace(result.Cookie))
            HttpContext.Session.SetString("ApiCookie", result.Cookie);

        if (result.Role!.Equals("Organizer",
            StringComparison.OrdinalIgnoreCase))
            return RedirectToAction("Index", "Organizer");

        if (result.Role.Equals("Participant",
            StringComparison.OrdinalIgnoreCase))
            return RedirectToAction("Index", "Participant");

        HttpContext.Session.Clear();
        ModelState.AddModelError("", "Your account has an unrecognised role.");
        return View(model);
    }

    public async Task<IActionResult> Logout()
    {
        var token = HttpContext.Session.GetString("ApiToken");
        var cookie = HttpContext.Session.GetString("ApiCookie");

        try
        {
            await _api.LogoutAsync(token, cookie);
        }
        catch
        {
            // Always clear the MVC session even if API logout fails.
        }

        HttpContext.Session.Clear();

        return RedirectToAction("Login");
    }
}