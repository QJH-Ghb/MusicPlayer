using System;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Authentication.Google;
using MusicPlayer.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// MVC
builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<MusicContext>(options =>   //AddDbContext：註冊資料庫服務，讓 Controller 可以用建構子注入（DI）
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));  //UseSqlServer()：指定使用 SQL Server（若是 MySQL、SQLite 就換成相應方法）

// 偵錯用：確認是否讀到設定值
var gid = builder.Configuration["Authentication:Google:ClientId"];
var gsec = builder.Configuration["Authentication:Google:ClientSecret"];
Console.WriteLine($"[CFG] Google ClientId={(string.IsNullOrEmpty(gid) ? "<NULL>" : gid.Substring(0, Math.Min(gid.Length, 8)) + "...")}");
Console.WriteLine($"[CFG] Google ClientSecret={(string.IsNullOrEmpty(gsec) ? "<NULL>" : "***")}");

// Authentication
builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        // "Cookies"
        // 不把 DefaultChallengeScheme 設成 Cookies；保留預設或在需要時指定
    })
    .AddCookie(options =>
    {
        options.LoginPath = "/Home/Login";
        options.LogoutPath = "/Home/Logout";
        options.AccessDeniedPath = "/Home/AccessDenied";
        options.SlidingExpiration = true;
    })
    // 暫存外部登入票據的 Cookie（ Controller 會用到 "External"）
    .AddCookie("External")
    // Google OAuth
    .AddGoogle(options =>
    {
        options.ClientId = builder.Configuration["Authentication:Google:ClientId"];
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
        // 讓 Google 將外部登入結果寫進 "External" 暫存 cookie
        options.SignInScheme = "External";
        options.SaveTokens = true;
        options.Scope.Add("email");
        options.Scope.Add("profile");
        // 如需自訂回呼路徑（對應你 Controller 的 Route），可解除註解
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

// 順序：Authentication -> Authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
    
app.Run();
