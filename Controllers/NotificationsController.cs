using System.Security.Claims;
using DiveMarketplace.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace DiveMarketplace.Controllers;
[Authorize]
public class NotificationsController(NotificationService notifications):Controller
{
    private int UserId=>int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    [HttpGet,ResponseCache(NoStore=true,Location=ResponseCacheLocation.None)]
    public async Task<IActionResult> Index(bool history=false) { ViewData["History"]=history; return View(await notifications.GetAsync(UserId,history)); }
    [HttpGet,ResponseCache(NoStore=true,Location=ResponseCacheLocation.None)]
    public async Task<IActionResult> List(bool history=false)=>PartialView("_NotificationList",await notifications.GetAsync(UserId,history));
    [HttpGet,ResponseCache(NoStore=true,Location=ResponseCacheLocation.None)]
    public async Task<IActionResult> Indicator()=>PartialView("_NotificationIndicator",await notifications.GetIndicatorAsync(UserId,User.IsInRole("Admin")));
    [HttpPost] public async Task<IActionResult> MarkRead(int id)
    {
        if(!await notifications.MarkReadAsync(id,UserId)) return NotFound();
        return RedirectToAction(nameof(Index));
    }
    [HttpPost] public async Task<IActionResult> MarkAllRead()
    {
        await notifications.MarkAllReadAsync(UserId); return RedirectToAction(nameof(Index));
    }
}
