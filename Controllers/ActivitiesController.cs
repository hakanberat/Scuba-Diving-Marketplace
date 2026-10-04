using DiveMarketplace.Services;
using DiveMarketplace.ViewModels;
using Microsoft.AspNetCore.Mvc;
namespace DiveMarketplace.Controllers;
public class ActivitiesController(ActivityService activities,ActivityCategoryService categories,PhotoService photos) : Controller
{
 public async Task<IActionResult> Index(ActivitySearchViewModel model)
 {
  if(model.MinPrice>model.MaxPrice) ModelState.AddModelError("","Minimum price must not exceed maximum price.");
  model.Categories=await categories.GetAllAsync();
  if(ModelState.IsValid) model.Activities=await activities.SearchAsync(model);
  return View(model);
 }
 public async Task<IActionResult> Details(int id)
 {
  var activity=await activities.GetPublicAsync(id);
  return activity is null ? NotFound() : View(new ActivityDetailsViewModel
  {
   Activity=activity,
   Photos=await photos.GetPublicAsync(DiveMarketplace.Models.PhotoTarget.Activity,id)
  });
 }
}
