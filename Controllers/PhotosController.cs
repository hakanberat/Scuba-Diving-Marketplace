using System.Security.Claims;
using DiveMarketplace.Models;
using DiveMarketplace.Services;
using DiveMarketplace.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DiveMarketplace.Controllers;

[Authorize(Roles = "DiveCenter,Admin")]
public class PhotosController(PhotoService photos, PhotoFileService files) : Controller
{
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private bool Admin => User.IsInRole("Admin");
    [HttpGet] public Task<IActionResult> Center(int id) => ManageAsync(PhotoTarget.Center, id);
    [HttpGet] public Task<IActionResult> Activity(int id) => ManageAsync(PhotoTarget.Activity, id);
    private async Task<IActionResult> ManageAsync(PhotoTarget target, int id)
    {
        var model = await photos.GetManagedAsync(target, id, UserId, Admin);
        return model is null ? Forbid() : View("Manage", model);
    }

    [HttpPost, RequestSizeLimit(101 * 1024 * 1024), RequestFormLimits(MultipartBodyLengthLimit = 101 * 1024 * 1024)]
    public async Task<IActionResult> Add([Bind(Prefix = "Input")] PhotoInputViewModel input)
    {
        if (!Enum.IsDefined(input.Target)) return BadRequest();
        var model = await photos.GetManagedAsync(input.Target, input.ResourceId, UserId, Admin);
        if (model is null) return Forbid();
        if (ModelState.IsValid)
        {
            var savedUrls = new List<string>();
            var uploaded=input.GetUploads().ToList();
            if(model.Photos.Count+Math.Max(1,uploaded.Count)>PhotoService.MaximumPhotos)
            {
                ModelState.AddModelError("", "Each gallery supports up to 20 photos. Remove some photos or select fewer files.");
                model.Input=input;
                return View("Manage",model);
            }
            try
            {
                foreach(var file in uploaded) savedUrls.Add(await files.SaveAsync(file));
                var urls=uploaded.Count>0 ? savedUrls : new List<string> {input.ImageUrl!.Trim()};
                if (await photos.AddManyAsync(input, urls, UserId, Admin))
                {
                    TempData["Success"] = urls.Count==1 ? "Photo added." : $"{urls.Count} photos added.";
                    return Back(input.Target, input.ResourceId);
                }
                ModelState.AddModelError("", "Photo could not be added. Each gallery supports up to 20 photos.");
            }
            catch (ArgumentException ex) { ModelState.AddModelError("", ex.Message); }
            catch { foreach(var url in savedUrls) files.Delete(url); throw; }
            foreach(var url in savedUrls) files.Delete(url);
        }
        // Browsers do not preserve file selections following a validation error.
        model.Input = input;
        return View("Manage", model);
    }

    [HttpPost] public async Task<IActionResult> SetCover(PhotoTarget target, int id, int photoId)
    {
        if (!ModelState.IsValid || !Enum.IsDefined(target)) return BadRequest();
        if (!await photos.SetCoverAsync(target, id, photoId, UserId, Admin)) return Forbid();
        TempData["Success"] = "Cover photo updated.";
        return Back(target, id);
    }
    [HttpPost] public async Task<IActionResult> Delete(PhotoTarget target, int id, int photoId)
    {
        if (!ModelState.IsValid || !Enum.IsDefined(target)) return BadRequest();
        var url = await photos.DeleteAsync(target, id, photoId, UserId, Admin);
        if (url is null) return Forbid();
        files.Delete(url);
        TempData["Success"] = "Photo removed.";
        return Back(target, id);
    }
    private IActionResult Back(PhotoTarget target, int id) => RedirectToAction(target == PhotoTarget.Center ? nameof(Center) : nameof(Activity), new { id });
}
