using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using DiveMarketplace.Models;
using DiveMarketplace.Services;
using DiveMarketplace.ViewModels;

namespace DiveMarketplace.Controllers;

public class HomeController(ActivityService activities, ActivityCategoryService categories, DiveCenterService centers) : Controller
{
    public async Task<IActionResult> Index()
    {
        return View(new HomeViewModel
        {
            Activities = (await activities.SearchAsync(new ActivitySearchViewModel())).Take(6).ToList(),
            Categories = await categories.GetAllAsync(),
            Centers = (await centers.GetAllAsync()).Take(3).ToList()
        });
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
