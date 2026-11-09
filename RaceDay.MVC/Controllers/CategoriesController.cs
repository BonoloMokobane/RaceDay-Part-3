
using Microsoft.AspNetCore.Mvc;
using RaceDay.MVC.Models;
using RaceDay.MVC.Services;

namespace RaceDay.MVC.Controllers;

public class CategoriesController : Controller
{
    private readonly RaceDayApiService _api;

    public CategoriesController(RaceDayApiService api)
    {
        _api = api;
    }

    private string? Token => HttpContext.Session.GetString("ApiToken");
    private string? Cookie => HttpContext.Session.GetString("ApiCookie");

    private bool IsOrganizer =>
        HttpContext.Session.GetString("UserRole") == "Organizer";

    public async Task<IActionResult> Index(int eventId)
    {
        if (!IsOrganizer)
            return RedirectToAction("Login", "Account");

        var categories = await _api.GetCategoriesAsync(
            eventId, Token, Cookie);

        ViewBag.EventId = eventId;
        return View(categories);
    }

    [HttpGet]
    public IActionResult Create(int eventId)
    {
        if (!IsOrganizer)
            return RedirectToAction("Login", "Account");

        return View(new CategoryViewModel { EventId = eventId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CategoryViewModel model)
    {
        if (!IsOrganizer)
            return RedirectToAction("Login", "Account");

        if (!ModelState.IsValid)
            return View(model);

        var result = await _api.CreateCategoryAsync(
            model.EventId, model, Token, Cookie);

        if (!result.Success)
        {
            ModelState.AddModelError("", result.Message);
            return View(model);
        }

        TempData["Success"] = "Category created successfully.";

        return RedirectToAction("Index",
            new { eventId = model.EventId });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        if (!IsOrganizer)
            return RedirectToAction("Login", "Account");

        var model = await _api.GetCategoryAsync(id, Token, Cookie);

        if (model == null)
            return NotFound();

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(CategoryViewModel model)
    {
        if (!IsOrganizer)
            return RedirectToAction("Login", "Account");

        if (!ModelState.IsValid)
            return View(model);

        var result = await _api.UpdateCategoryAsync(
            model, Token, Cookie);

        if (!result.Success)
        {
            ModelState.AddModelError("", result.Message);
            return View(model);
        }

        TempData["Success"] = "Category updated successfully.";

        return RedirectToAction("Index",
            new { eventId = model.EventId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, int eventId)
    {
        if (!IsOrganizer)
            return RedirectToAction("Login", "Account");

        var result = await _api.DeleteCategoryAsync(
            id, Token, Cookie);

        TempData["Success"] = result.Success
            ? "Category deleted."
            : result.Message;

        return RedirectToAction("Index", new { eventId });
    }
}
