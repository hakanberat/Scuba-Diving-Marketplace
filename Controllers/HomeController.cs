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
            .Concat(diveCenters.Select(c=>new DestinationViewModel {Country=c.Country??"",City=c.City,ImageUrl=c.DisplayImageUrl}))
            .Where(d=>!string.IsNullOrWhiteSpace(d.Country))
            .GroupBy(d=>new {d.Country,d.City})
            .Select(g=>g.FirstOrDefault(d=>!string.IsNullOrWhiteSpace(d.ImageUrl))??g.First())
            .ToList();
        var categoryPriority = new[] { "Try Dive", "Scuba Course", "Fun Dive", "Boat Dive", "Shore Dive", "Wreck Dive", "Night Dive", "Liveaboard" };
        var categoryGroups = experiences.GroupBy(a => a.ActivityCategoryId).ToDictionary(g => g.Key, g => g.ToList());
        var featuredCategories = (await categories.GetAllAsync())
            .Where(c => !string.Equals(c.Name.Trim(), "Other", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(c => categoryGroups.GetValueOrDefault(c.Id)?.Count ?? 0)
            .ThenBy(c => { var rank = Array.FindIndex(categoryPriority, name => string.Equals(name, c.Name, StringComparison.OrdinalIgnoreCase)); return rank < 0 ? int.MaxValue : rank; })
            .ThenBy(c => c.Name)
            .Take(8).ToList();
        return View(new HomeViewModel
        {
            Destinations = destinations,
            Activities = experiences.Take(6).ToList(),
            Categories = featuredCategories,
            CategoryImages = categoryGroups.ToDictionary(g => g.Key, g => g.Value.FirstOrDefault(a => !string.IsNullOrWhiteSpace(a.DisplayImageUrl))?.DisplayImageUrl),
            CategoryActivityCounts = categoryGroups.ToDictionary(g => g.Key, g => g.Value.Count),
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
