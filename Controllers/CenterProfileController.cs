using System.Security.Claims;
using DiveMarketplace.Services;
using DiveMarketplace.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace DiveMarketplace.Controllers;
[Authorize]
public class CenterProfileController(CenterAccessService access,AppUserService users) : Controller
{
 private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
 private bool Admin => User.IsInRole("Admin");
 [Authorize(Roles="DiveCenter,Admin")]
 public async Task<IActionResult> Index() => View(await access.GetCentersAsync(UserId,Admin));
 [Authorize(Roles="Admin"),HttpGet] public IActionResult Create() => View("Edit",new CenterEditViewModel());
 [Authorize(Roles="Admin"),HttpPost] public async Task<IActionResult> Create(CenterEditViewModel model)
 {
  model.Id=0;
  if(string.IsNullOrWhiteSpace(model.MapLocation)) ModelState.AddModelError(nameof(model.MapLocation),"Enter the school Google Maps link or precise location.");
  if(!ModelState.IsValid) return View("Edit",model);
  var id=await access.CreateAsync(model,UserId);
  var user=await users.GetByIdAsync(UserId);
  await AccountController.SignInAsync(HttpContext,user!);
  TempData["Success"]="Your dive center profile is ready. Add instructors and activities from your dashboard.";
  return RedirectToAction(nameof(Edit),new {id});
 }
 [Authorize(Roles="DiveCenter,Admin"),HttpGet]
 public async Task<IActionResult> Edit(int id)
 {
  var model=await access.GetProfileAsync(id,UserId,Admin);
  return model is null ? Forbid() : View(model);
 }
 [Authorize(Roles="DiveCenter,Admin"),HttpPost]
 public async Task<IActionResult> Edit(int id,CenterEditViewModel model)
 {
  if(id!=model.Id) return BadRequest();
  if(!await access.CanManageAsync(id,UserId,Admin)) return Forbid();
  if(!ModelState.IsValid) return View(model);
  if(!await access.SaveProfileAsync(model,UserId,Admin)) return Forbid();
  TempData["Success"]="Profile saved.";
  return RedirectToAction(nameof(Index));
 }
}
