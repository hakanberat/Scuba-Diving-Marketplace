using DiveMarketplace.Data;
using DiveMarketplace.Services;
using DiveMarketplace.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews(options =>
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));
builder.Services.AddSingleton<SqlConnectionFactory>();
builder.Services.AddScoped<DiveCenterService>();
builder.Services.AddScoped<PhotoService>();
builder.Services.AddScoped<PhotoFileService>();
builder.Services.AddScoped<AppUserService>();
builder.Services.AddScoped<CenterApplicationService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<CenterAccessService>();
builder.Services.AddScoped<ActivityCategoryService>();
builder.Services.AddScoped<ActivityService>();
builder.Services.AddScoped<InstructorService>();
builder.Services.AddScoped<ReservationService>();
builder.Services.AddScoped<DashboardService>();
builder.Services.AddScoped<AdminService>();
builder.Services.AddScoped<AccountCookieEvents>();
builder.Services.AddScoped<IPasswordHasher<AppUser>, PasswordHasher<AppUser>>();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.EventsType = typeof(AccountCookieEvents);
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
