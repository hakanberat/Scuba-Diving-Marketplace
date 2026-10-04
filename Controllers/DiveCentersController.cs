using DiveMarketplace.Services;
using DiveMarketplace.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace DiveMarketplace.Controllers;

public class DiveCentersController : Controller
{
    private readonly DiveCenterService _diveCenterService;
    private readonly InstructorService _instructors;
    private readonly ActivityService _activities;
    private readonly PhotoService _photos;

    public DiveCentersController(DiveCenterService diveCenterService, InstructorService instructors, ActivityService activities, PhotoService photos)
    {
        _diveCenterService = diveCenterService;
        _instructors = instructors;
        _activities = activities;
        _photos = photos;
    }

    public async Task<IActionResult> Index()
    {
        var diveCenters = await _diveCenterService.GetAllAsync();

        return View(diveCenters);
    }

    public async Task<IActionResult> Details(int id)
    {
        var diveCenter = await _diveCenterService.GetByIdAsync(id);

        if (diveCenter is null)
        {
            return NotFound();
        }

        return View(new DiveCenterDetailsViewModel
        {
            Center = diveCenter,
            Photos = await _photos.GetPublicAsync(DiveMarketplace.Models.PhotoTarget.Center, id),
            Instructors = await _instructors.GetPublicAsync(id),
            Activities = await _activities.SearchAsync(new ActivitySearchViewModel(), id)
        });
    }
}
