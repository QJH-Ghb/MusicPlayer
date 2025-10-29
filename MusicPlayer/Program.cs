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
builder.Services.AddDbContext<MusicContext>(options =>   //AddDbContext�G���U��Ʈw�A�ȡA�� Controller �i�H�Ϋغc�l�`�J�]DI�^
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));  //UseSqlServer()�G���w�ϥ� SQL Server�]�Y�O MySQL�BSQLite �N����������k�^

// �����ΡG�T�{�O�_Ū��]�w��
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
        // ���� DefaultChallengeScheme �]�� Cookies�F�O�d�w�]�Φb�ݭn�ɫ��w
    })
    .AddCookie(options =>
    {
        options.LoginPath = "/Home/Login";
        options.LogoutPath = "/Home/Logout";
        options.AccessDeniedPath = "/Home/AccessDenied";
        options.SlidingExpiration = true;
    })
    // �Ȧs�~���n�J���ڪ� Cookie�] Controller �|�Ψ� "External"�^
    .AddCookie("External")
    // Google OAuth
    .AddGoogle(options =>
    {
        options.ClientId = builder.Configuration["Authentication:Google:ClientId"];
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
        // �� Google �N�~���n�J���G�g�i "External" �Ȧs cookie
        options.SignInScheme = "External";
        options.SaveTokens = true;
        options.Scope.Add("email");
        options.Scope.Add("profile");
        // �p�ݦۭq�^�I���|�]�����A Controller �� Route�^�A�i�Ѱ�����
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

// ���ǡGAuthentication -> Authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
    
app.Run();
