using System.Security.Claims;
using DiveMarketplace.Services;
using DiveMarketplace.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace DiveMarketplace.Controllers;
[Authorize(Roles="DiveCenter,Admin")]
public class ManageActivitiesController(ActivityService activities,ActivityCategoryService categories,CenterAccessService access) : Controller
{
 private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
 private bool Admin => User.IsInRole("Admin");
 public async Task<IActionResult> Index() => View(await activities.GetManagedAsync(UserId,Admin));
 [HttpGet] public async Task<IActionResult> Create()
 {
  var model=new ActivityEditViewModel(); await PopulateAsync(model);
  model.DiveCenterId=model.Centers.FirstOrDefault()?.Id ?? 0;
  return View("Edit",model);
 }
 [HttpGet] public async Task<IActionResult> Edit(int id)
 {
  var model=await activities.GetEditAsync(id,UserId,Admin);
  if(model is null) return Forbid();
  await PopulateAsync(model); return View(model);
 }
 [HttpPost] public Task<IActionResult> Create(ActivityEditViewModel model) { model.Id=0; return SaveAsync(model); }
 [HttpPost] public async Task<IActionResult> Edit(int id,ActivityEditViewModel model)
 {
  if(id!=model.Id) return BadRequest();
  var existing=await activities.GetEditAsync(id,UserId,Admin);
  if(existing is null || existing.DiveCenterId!=model.DiveCenterId) return Forbid();
  return await SaveAsync(model);
 }
 private async Task<IActionResult> SaveAsync(ActivityEditViewModel model)
 {
  if(!await access.CanManageAsync(model.DiveCenterId,UserId,Admin)) return Forbid();
  if(ModelState.IsValid && await activities.SaveAsync(model,UserId,Admin)) { TempData["Success"]="Activity saved.";return RedirectToAction(nameof(Index)); }
  if(ModelState.IsValid) ModelState.AddModelError("","Select a valid activity category.");
  await PopulateAsync(model); return View("Edit",model);
 }
 private async Task PopulateAsync(ActivityEditViewModel model)
 {
  model.Centers=await access.GetCentersAsync(UserId,Admin); model.Categories=await categories.GetAllAsync(true);
 }
 [HttpPost] public async Task<IActionResult> SetActive(int id,bool active)
 {
  if(!await activities.SetActiveAsync(id,active,UserId,Admin)) return Forbid();
  return RedirectToAction(nameof(Index));
 }
}