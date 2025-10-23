using System;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Authentication.Google;

var builder = WebApplication.CreateBuilder(args);

// MVC
builder.Services.AddControllersWithViews();

// （可選）偵錯用：確認是否讀到設定值
var gid = builder.Configuration["Authentication:Google:ClientId"];
var gsec = builder.Configuration["Authentication:Google:ClientSecret"];
Console.WriteLine($"[CFG] Google ClientId={(string.IsNullOrEmpty(gid) ? "<NULL>" : gid.Substring(0, Math.Min(gid.Length, 8)) + "...")}");
Console.WriteLine($"[CFG] Google ClientSecret={(string.IsNullOrEmpty(gsec) ? "<NULL>" : "***")}");

// Authentication
builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme; // "Cookies"
        // 不要把 DefaultChallengeScheme 設成 Cookies；保留預設或在需要時指定
    })
    .AddCookie(options =>
    {
        options.LoginPath = "/Home/Login";
        options.LogoutPath = "/Home/Logout";
        options.SlidingExpiration = true;
    })
    .AddGoogle(options =>
    {
        options.ClientId = builder.Configuration["Authentication:Google:ClientId"];
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
        options.SaveTokens = true;
        options.Scope.Add("email");
        options.Scope.Add("profile");
        // options.CallbackPath = "/externallogin-callback";
    });

builder.Services.AddAuthorization();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Login}/{id?}");

app.Run();
