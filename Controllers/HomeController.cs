using System.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using VotingApp.Web.Models;

namespace VotingApp.Web.Controllers;

/// <summary>
/// Home controller for general information pages
/// </summary>
public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly SignInManager<AppUser> _signInManager;

    public HomeController(ILogger<HomeController> logger, SignInManager<AppUser> signInManager)
    {
        _logger = logger;
        _signInManager = signInManager;
    }

    /// <summary>
    /// Landing page of the app, for non-registered users.
    /// </summary>
    public IActionResult Index()
    {
        // If the user is signed in, redirect to their dashboard automatically
        if (_signInManager.IsSignedIn(User))
        {
            return RedirectToAction("Index", "Vote");
        }
        // Otherwise, show the default home page
        return View();
    }

    /// <summary>
    /// Data privacy statement page.
    /// </summary>
    public IActionResult Privacy()
    {
        return View();
    }

    /// <summary>
    /// Rules list page.
    /// </summary>
    public IActionResult Rules()
    {
        return View();
    }

    /// <summary>
    /// Main error page.
    /// </summary>
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
