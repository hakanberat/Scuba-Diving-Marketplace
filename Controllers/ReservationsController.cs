using System.Security.Claims;
using DiveMarketplace.Services;
using DiveMarketplace.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace DiveMarketplace.Controllers;
[Authorize]
public class ReservationsController(ReservationService reservations,ActivityService activities,AppUserService users) : Controller
{
 private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
 private bool Admin => User.IsInRole("Admin");
 private bool Manager => User.IsInRole("DiveCenter") || Admin;
 [Authorize(Roles="Customer"),HttpGet] public async Task<IActionResult> Create(int activityId)
 {
  var activity=await activities.GetPublicAsync(activityId);
  if(activity is null) return NotFound();
  var user=await users.GetByIdAsync(UserId);
  return View(new ReservationCreateViewModel {ActivityId=activityId,Activity=activity,ContactEmail=user!.Email,ContactPhone=user.Phone??""});
 }
 [Authorize(Roles="Customer"),HttpPost] public async Task<IActionResult> Create(ReservationCreateViewModel model)
 {
  model.Activity=await activities.GetPublicAsync(model.ActivityId);
  if(model.Activity is null) return NotFound();
  if(model.ReservationDate.Date<DateTime.Today) ModelState.AddModelError(nameof(model.ReservationDate),"Choose today or a future date.");
  if(model.Activity.MaxParticipants.HasValue && model.Participants>model.Activity.MaxParticipants) ModelState.AddModelError(nameof(model.Participants),"Participant count exceeds the activity limit.");
  if(!ModelState.IsValid) return View(model);
  var id=await reservations.CreateAsync(model,UserId);
  if(id is null) { ModelState.AddModelError("","This activity is no longer available or the participant limit has changed."); return View(model); }
  TempData["Success"]="Reservation submitted. The dive center will review your request.";
  return RedirectToAction(nameof(Details),new {id});
 }
 public async Task<IActionResult> MyReservations() => View("Index",await reservations.GetCustomerAsync(UserId));
 [Authorize(Roles="DiveCenter,Admin")]
 public async Task<IActionResult> Index(string? status=null)
 {
  if(status is not (null or "Pending" or "Confirmed" or "Rejected" or "Cancelled")) return BadRequest();
  ViewData["Management"]=true; ViewData["Status"]=status;
  return View(await reservations.GetManagedAsync(UserId,Admin,status));
 }
 public async Task<IActionResult> Details(int id)
 {
  var reservation=await reservations.GetAsync(id,UserId,Admin,Manager);
  return reservation is null ? NotFound() : View(reservation);
 }
 [HttpPost] public async Task<IActionResult> ChangeStatus(int id,string status,string? notes)
 {
  if(!await reservations.ChangeStatusAsync(id,status,notes,UserId,Admin,Manager)) return BadRequest("This transition is unavailable or you cannot manage this reservation.");
  TempData["Success"]="Reservation status updated.";
  return RedirectToAction(nameof(Details),new {id});
 }
}
