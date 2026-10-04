using System.Security.Claims;
using DiveMarketplace.Services;
using DiveMarketplace.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
namespace DiveMarketplace.Controllers;
[Authorize(Roles="Admin")]
public class AdminController(AdminService admin,AppUserService users,ActivityCategoryService categories,CenterAccessService access,CenterApplicationService applications) : Controller
{
 public async Task<IActionResult> Index() => View(await admin.GetOverviewAsync());
 public async Task<IActionResult> Centers() => View(await admin.GetCentersAsync());
 public async Task<IActionResult> Applications() => View(await applications.GetAllAsync());
 [HttpGet] public async Task<IActionResult> Application(int id)
 {
  var model=await applications.GetAsync(id);
  return model is null ? NotFound() : View(model);
 }
 [HttpPost] public async Task<IActionResult> ReviewApplication(int id,bool approve,string? notes)
 {
  if(notes?.Length>2000 || !ModelState.IsValid) return BadRequest();
  if(!await applications.ReviewAsync(id,approve,notes,int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!)))
   return BadRequest("This application is no longer pending or its account is inactive.");
  TempData["Success"]=approve?"Application approved. The owner can now manage their center.":"Application rejected.";
  return RedirectToAction(nameof(Application),new {id});
 }
 [HttpPost] public async Task<IActionResult> SetCenterActive(int id,bool active)
 {
  if(!await admin.SetCenterActiveAsync(id,active)) return NotFound();
  return RedirectToAction(nameof(Centers));
 }
 public async Task<IActionResult> Owners() { ViewData["Owners"]=true; return View("Users",(await users.GetAllAsync()).Where(x=>x.Role=="DiveCenter").ToList()); }
 public async Task<IActionResult> Users() => View(await users.GetAllAsync());
 [HttpGet] public async Task<IActionResult> EditUser(int id)
 {
  var user=await users.GetByIdAsync(id); if(user is null) return NotFound();
  return View(new UserEditViewModel {Id=id,Name=user.Email,Role=user.Role,IsActive=user.IsActive});
 }
 [HttpPost] public async Task<IActionResult> EditUser(int id,UserEditViewModel model)
 {
  if(id!=model.Id) return BadRequest();
  if(!ModelState.IsValid) return View(model);
  if(!await users.UpdateAsync(model,int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!))) { ModelState.AddModelError("","You cannot modify your own admin role or activation status.");return View(model); }
  return RedirectToAction(nameof(Users));
 }
 public async Task<IActionResult> Categories() => View(await categories.GetAllAsync(true));
 [HttpGet] public async Task<IActionResult> EditCategory(int id=0)
 {
  var model=id==0 ? new CategoryEditViewModel() : await categories.GetAsync(id);
  return model is null ? NotFound() : View(model);
 }
 [HttpPost] public async Task<IActionResult> EditCategory(int id,CategoryEditViewModel model)
 {
  if(id!=model.Id) return BadRequest();
  if(!ModelState.IsValid) return View(model);
  if(id!=0 && await categories.GetAsync(id) is null) return NotFound();
  try { await categories.SaveAsync(model); }
  catch(SqlException ex) when(ex.Number is 2601 or 2627) { ModelState.AddModelError(nameof(model.Slug),"This slug already exists.");return View(model); }
  return RedirectToAction(nameof(Categories));
 }
 [HttpGet] public async Task<IActionResult> AssignCenterUser()
 {
  var model=new CenterUserViewModel(); await PopulateAsync(model);return View(model);
 }
 [HttpPost] public async Task<IActionResult> AssignCenterUser(CenterUserViewModel model)
 {
  if(ModelState.IsValid && await access.AssignAsync(model)) { TempData["Success"]="Dive center access assigned.";return RedirectToAction(nameof(Centers)); }
  ModelState.AddModelError("","Select an active DiveCenter user and a valid dive center.");await PopulateAsync(model);return View(model);
 }
 private async Task PopulateAsync(CenterUserViewModel model)
 {
  model.Centers=await admin.GetCentersAsync();model.Users=(await users.GetAllAsync()).Where(x=>x.Role=="DiveCenter" && x.IsActive).ToList();
 }
}
