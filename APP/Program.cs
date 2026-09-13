using APP.Attributes;
using APP.Excel;
using APP.Models;
using APP.Services.Implementations;
using APP.Services.Interfaces;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();

builder.Services.AddControllersWithViews(options =>
{
    // Global gate so a self-service employee can never reach an admin/HR
    // page by URL, even if a specific controller forgot to add its own
    // guard - see EssRestrictionAttribute for the allow/deny lists.
    options.Filters.Add<EssRestrictionAttribute>();

    // Global safety net so a genuinely-expired session (refresh token
    // fully expired or revoked) always lands the user on a clean Login
    // page with a friendly message, instead of the generic /Home/Error
    // page an unhandled UnauthorizedAccessException from ApiService would
    // otherwise hit - see ApiSessionExpiredFilter.
    options.Filters.Add<ApiSessionExpiredFilter>();
})
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
    });

// Session is backed by IDistributedCache - AddSession() does NOT register
// one for you, and without it the session store either silently falls
// back to per-process memory with no persistence guarantees, or throws
// once something actually touches Session. Registered explicitly so this
// is never left to implicit/undocumented framework behavior. (For a
// multi-instance/load-balanced production deployment, swap this for
// AddStackExchangeRedisCache/AddDistributedSqlServerCache so a session
// survives hitting a different server instance - single-server in-memory
// is fine for this app's current deployment.)
builder.Services.AddDistributedMemoryCache();

// Default AddSession() uses a 20-minute sliding IdleTimeout, which was
// silently logging users out of the ERP after 20 minutes of inactivity
// (e.g. reading a report, being in a meeting) even though nothing about
// that should count as "logging out". Extended to match the server-side
// RefreshToken lifetime (30 days, see AuthService.GenerateAuthResponse /
// Jwt:RefreshTokenExpiryDays) so the session store itself isn't the thing
// kicking people out - the refresh-token flow in JwtAuthorizeAttribute is
// what actually governs how long a login stays valid, and an explicit
// Logout is what ends it.
//
// Cookie.MaxAge is just as important as IdleTimeout: without it, ASP.NET
// Core issues the session cookie as a browser-session cookie (no
// Expires/Max-Age at all), which the browser deletes the moment it's
// closed - regardless of how long the *server-side* session would have
// stayed valid. That's what made "close and reopen the browser" behave
// like an explicit logout even though nothing revoked the session.
// Setting MaxAge makes it a real persistent cookie.
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromDays(30);
    options.Cookie.IsEssential = true;
    options.Cookie.HttpOnly = true;
    options.Cookie.MaxAge = TimeSpan.FromDays(30);
    options.Cookie.SameSite = SameSiteMode.Lax;
});

builder.Services.AddHttpContextAccessor();

// Data Protection is what encrypts the session cookie and the antiforgery
// token - ASP.NET Core registers a default provider automatically even
// though there's no explicit AddDataProtection() call, but its default key
// storage location depends on a loaded user profile, which the
// ApplicationPoolIdentity IIS runs this app as does NOT have. Without
// this, IIS was silently generating a brand new key ring on every app
// pool recycle - which invalidated every existing session cookie and
// antiforgery token, surfacing as "The key {...} was not found in the key
// ring" / "The antiforgery token could not be decrypted" and a hard-to-
// diagnose 500 on any POST (edit/delete/create form submit) shortly after
// a recycle. Keys are stored under App_Data\Keys, next to the app, so the
// IIS Application Pool identity needs Modify rights there (see deployment
// guide's folder permissions section).
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(
        Path.Combine(builder.Environment.ContentRootPath, "App_Data", "Keys")))
    .SetApplicationName("HRMS-ERP-APP");

builder.Services.AddHttpClient<IApiService, ApiService>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ApiSettings:BaseUrl"]);
    client.Timeout = TimeSpan.FromMinutes(5);
});

builder.Services.Configure<AppSettings>(
    builder.Configuration.GetSection("AppSettings"));

// Generic Excel import/export engine (APP/Excel) - shared by every
// module's Import/Export/DownloadImportTemplate actions instead of each
// one hand-rolling its own ClosedXML workbook code.
builder.Services.AddScoped<IExcelEngine, ExcelEngine>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// The IIS site (see deployment guide) is bound to HTTP only
// (http://localhost:8080), so forcing a redirect to HTTPS in Production
// would send every request to a binding that doesn't exist and break the
// app. Kept for Development, where the "https" launchSettings profile
// provides a real HTTPS endpoint to redirect to. If you later add an
// HTTPS binding in IIS, move this back outside the check.
if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseStaticFiles();

app.UseRouting();

app.UseSession();

app.MapRazorPages();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Auth}/{action=Login}/{id?}")
    .WithStaticAssets();


app.Run();
