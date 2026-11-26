using System.Security.Claims;
using DiveMarketplace.Services;
using DiveMarketplace.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace DiveMarketplace.Controllers;
[Authorize(Roles="DiveCenter,Admin")]
public class InstructorsController(InstructorService instructors,CenterAccessService access) : Controller
{
 private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
 private bool Admin => User.IsInRole("Admin");
 public async Task<IActionResult> Index() => View(await instructors.GetManagedAsync(UserId,Admin));
 [HttpGet] public async Task<IActionResult> Create()
 {
  var model=new InstructorEditViewModel {Centers=await access.GetCentersAsync(UserId,Admin)};
  model.DiveCenterId=model.Centers.FirstOrDefault()?.Id ?? 0;
  return View("Edit",model);
 }
 [HttpGet] public async Task<IActionResult> Edit(int id)
 {
  var model=await instructors.GetEditAsync(id,UserId,Admin);
  if(model is null) return Forbid();
  model.Centers=await access.GetCentersAsync(UserId,Admin); return View(model);
 }
 [HttpPost] public Task<IActionResult> Create(InstructorEditViewModel model) { model.Id=0; return SaveAsync(model); }
 [HttpPost] public async Task<IActionResult> Edit(int id,InstructorEditViewModel model)
 {
  if(id!=model.Id) return BadRequest();
  var existing=await instructors.GetEditAsync(id,UserId,Admin);
  if(existing is null || existing.DiveCenterId!=model.DiveCenterId) return Forbid();
  return await SaveAsync(model);
 }
 private async Task<IActionResult> SaveAsync(InstructorEditViewModel model)
 {
  if(!await access.CanManageAsync(model.DiveCenterId,UserId,Admin)) return Forbid();
  if(ModelState.IsValid && await instructors.SaveAsync(model,UserId,Admin)) return RedirectToAction(nameof(Index));
  model.Centers=await access.GetCentersAsync(UserId,Admin); return View("Edit",model);
 }
 [HttpPost] public async Task<IActionResult> SetActive(int id,bool active)
 {
  if(!await instructors.SetActiveAsync(id,active,UserId,Admin)) return Forbid();
  return RedirectToAction(nameof(Index));
 }
}