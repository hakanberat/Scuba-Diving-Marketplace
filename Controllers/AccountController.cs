using System.Security.Claims;
using DiveMarketplace.Models;
using DiveMarketplace.Services;
using DiveMarketplace.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace DiveMarketplace.Controllers;
public class AccountController(AppUserService users,CenterApplicationService applications) : Controller
{
 [Authorize,HttpGet] public async Task<IActionResult> Profile()
 {
  var user=await users.GetByIdAsync(int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!));
  return user is null ? NotFound() : View(new ProfileViewModel {FirstName=user.FirstName,LastName=user.LastName,Email=user.Email,Phone=user.Phone});
 }
 [Authorize,HttpPost] public async Task<IActionResult> Profile(ProfileViewModel model)
 {
  if(!ModelState.IsValid) return View(model);
  var id=int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
  if(!await users.SaveProfileAsync(id,model)) { ModelState.AddModelError(nameof(model.Email),"This email is already in use or your account is unavailable."); return View(model); }
  await SignInAsync(HttpContext,(await users.GetByIdAsync(id))!);
  TempData["Success"]="Your profile has been updated.";
  return RedirectToAction(nameof(Profile));
 }
 [AllowAnonymous, HttpGet] public IActionResult Login(string? returnUrl=null) => View(new LoginViewModel {ReturnUrl=returnUrl});
 [AllowAnonymous, HttpPost] public async Task<IActionResult> Login(LoginViewModel model)
 {
  if(!ModelState.IsValid) return View(model);
  var user=await users.AuthenticateAsync(model.Email,model.Password);
  if(user is null) { ModelState.AddModelError("","Invalid email or password."); return View(model); }
  await SignInAsync(HttpContext,user);
  return Url.IsLocalUrl(model.ReturnUrl) ? LocalRedirect(model.ReturnUrl!) : RedirectToAction("Index","Home");
 }
 [AllowAnonymous,HttpGet] public IActionResult Register(string? returnUrl=null) => View(new RegisterViewModel {ReturnUrl=returnUrl});
 [AllowAnonymous,HttpPost] public async Task<IActionResult> Register(RegisterViewModel model)
 {
  if(string.IsNullOrEmpty(model.Password) || model.Password.Length<10 || model.Password.Length>128) ModelState.AddModelError(nameof(model.Password),"Use a password between 10 and 128 characters.");
  if(string.IsNullOrWhiteSpace(model.FirstName) || string.IsNullOrWhiteSpace(model.LastName)) ModelState.AddModelError("","Enter your first and last name.");
  if(!ModelState.IsValid) return View(model);
  var user=await users.RegisterAsync(model);
  if(user is null) { ModelState.AddModelError(nameof(model.Email),"An account with this email already exists."); return View(model); }
  await SignInAsync(HttpContext,user);
  return RedirectToAction(nameof(RegistrationSuccess),new {returnUrl=Url.IsLocalUrl(model.ReturnUrl) ? model.ReturnUrl : null});
 }
 [Authorize,HttpGet] public IActionResult RegistrationSuccess(string? returnUrl=null)
 {
  ViewData["ReturnUrl"]=Url.IsLocalUrl(returnUrl) ? returnUrl : Url.Action("Index","Home");
  return View();
 }
 [Authorize,HttpPost] public async Task<IActionResult> Logout()
 {
  await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
  return RedirectToAction("Index","Home");
 }
 [AllowAnonymous,HttpGet] public IActionResult RegisterOwner() => View(new OwnerRegistrationViewModel());
 [AllowAnonymous,HttpPost] public async Task<IActionResult> RegisterOwner(OwnerRegistrationViewModel model)
 {
  if(User.Identity?.IsAuthenticated==true) { ModelState.AddModelError("","Please log out before registering a separate owner account."); return View(model); }
  if(string.IsNullOrEmpty(model.Password) || model.Password.Length<10 || model.Password.Length>128) ModelState.AddModelError(nameof(model.Password),"Use a password between 10 and 128 characters.");
  if(!ModelState.IsValid) return View(model);
  var user=await applications.RegisterAsync(model);
  if(user is null) { ModelState.AddModelError(nameof(model.Email),"An account with this email already exists. Use a separate email for your owner account."); return View(model); }
  await SignInAsync(HttpContext,user);
  return RedirectToAction(nameof(ApplicationStatus));
 }
 [Authorize(Roles="DiveCenter"),HttpGet] public async Task<IActionResult> ApplicationStatus()
 {
  var application=await applications.GetForUserAsync(int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!));
  if(application is null) return RedirectToAction("Index","Dashboard");
  return View(application);
 }
 [HttpGet] public IActionResult AccessDenied() { Response.StatusCode=403; return View(); }
 public static Task SignInAsync(HttpContext context,AppUser user)
 {
  var identity=new ClaimsIdentity(new[] {
   new Claim(ClaimTypes.NameIdentifier,user.Id.ToString()),new Claim(ClaimTypes.Name,$"{user.FirstName} {user.LastName}"),
   new Claim(ClaimTypes.Email,user.Email),new Claim(ClaimTypes.Role,user.Role)
  },CookieAuthenticationDefaults.AuthenticationScheme);
  return context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,new ClaimsPrincipal(identity));
 }
}
