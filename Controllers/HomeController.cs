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
        var experiences=await activities.SearchAsync(new ActivitySearchViewModel());
        var diveCenters=(await centers.GetAllAsync()).ToList();
        var destinations=experiences.Select(a=>new DestinationViewModel {Country=a.Country??"",City=a.City,ImageUrl=a.DisplayImageUrl})
            .Concat(diveCenters.Select(c=>new DestinationViewModel {Country=c.Country??"",City=c.City,ImageUrl=c.CoverPhotoUrl}))
            .Where(d=>!string.IsNullOrWhiteSpace(d.Country))
            .GroupBy(d=>new {d.Country,d.City})
            .Select(g=>g.FirstOrDefault(d=>!string.IsNullOrWhiteSpace(d.ImageUrl))??g.First())
            .ToList();
        return View(new HomeViewModel
        {
            Destinations = destinations,
            Activities = experiences.Take(6).ToList(),
            Categories = await categories.GetAllAsync(),
            Centers = diveCenters.Take(3).ToList()
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
