using System.Security.Claims;
using DiveMarketplace.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace DiveMarketplace.Controllers;
[Authorize]
public class DashboardController(DashboardService dashboard,CenterApplicationService applications) : Controller
{
 public async Task<IActionResult> Index()
 {
  if(User.IsInRole("Customer")) return RedirectToAction("MyReservations","Reservations");
  if(User.IsInRole("DiveCenter"))
  {
   var application=await applications.GetForUserAsync(int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!));
   if(application is not null && application.Status!="Approved") return RedirectToAction("ApplicationStatus","Account");
  }
  return View(await dashboard.GetAsync(int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!),User.IsInRole("Admin")));
 }
}
