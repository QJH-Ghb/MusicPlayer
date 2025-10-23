using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.DataAnnotations;
using MusicPlayer.Models;
using MVC_DB_.Models;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace MusicPlayer.Controllers
{
    public class HomeController : Controller
    {

        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }
        [HttpGet]
        public IActionResult Login()
        {
            return View("~/Views/Login/Login.cshtml");
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous] // 建議標註，確保未登入也能打這個 Action
        public async Task<IActionResult> Login(string username, string password)
        {
            bool isAjax =
                string.Equals(Request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase) ||
                (Request.Headers["Accept"].ToString()?.IndexOf("application/json", StringComparison.OrdinalIgnoreCase) ?? -1) >= 0;

            var svc = new Logincheck();
            var result = svc.Login(username, password);

            if (!result.Success)
            {
                if (isAjax) return Json(new { success = false, message = result.Message });
                ViewBag.Message = result.Message;
                return View("~/Views/Login/Login.cshtml");
            }

            // 登入成功：建立 Claims
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, result.UserId.ToString()), // 依你的回傳調整
                new Claim(ClaimTypes.Name, username),
                new Claim("avatar", result.Avatar ? "true" : "false")
            };
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme); // "Cookies"
            var principal = new ClaimsPrincipal(identity);

            // 寫入驗證 Cookie
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme, // 必須與註冊的 scheme 一致
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = true,                                  // 若有「記住我」再用參數控制
                    ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
                });

            // 回應
            if (isAjax)
                return Json(new { success = true, redirectUrl = Url.Action("Index", "Home") });

            // 避免重整造成表單重送
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Register(string username, string password, string email)
        {
            bool isAjax =
                string.Equals(Request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase) ||
                (Request.Headers["Accept"].ToString()?.IndexOf("application/json", StringComparison.OrdinalIgnoreCase) ?? -1) >= 0;

            var svc = new Logincheck();
            var result = svc.Register(username, password, email);

            if (isAjax)
            {
                if (result.Success)
                    return Json(new { success = true, message = result.Message, redirectUrl = Url.Action("Login", "Home") });
                return Json(new { success = false, message = result.Message });
            }

            if (result.Success)
            {
                TempData["AlertMessage"] = result.Message ?? "註冊成功！請登入";
                return RedirectToAction("Login", "Home");
            }

            ViewBag.Message = result.Message;
            return View("~/Views/Login/Login.cshtml");
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ExternalLogin(string provider, string returnUrl = "/Home/Index")
        {
            // Google 完成後，會先回 /signin-google（由中介軟體處理）
            // 處理完再把使用者 redirect 到這裡設定的 RedirectUri（即我們自己的 Callback）
            var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Home", new { returnUrl });
            var props = new AuthenticationProperties { RedirectUri = redirectUrl };
            return Challenge(props, provider);
        }

        // 我們自己的 Callback（不是給 Google 的）
        // Google 先回 /signin-google -> 中介軟體處理後再導到這裡
        [HttpGet]
        [AllowAnonymous]
        [Route("externallogin-callback")]
        public async Task<IActionResult> ExternalLoginCallback(string returnUrl = "/Home/Index")
        {
            // 此時使用者已經由 Cookie Scheme 登入（Program.cs 設了 DefaultScheme = Cookies）
            // 你可以從 HttpContext.User 取到 claims；此處示範最小檢查
            if (!(User?.Identity?.IsAuthenticated ?? false))
            {
                TempData["AlertMessage"] = "外部登入失敗，請再試一次。";
                return RedirectToAction("Login");
            }

            return LocalRedirect(returnUrl);
        }

        [Authorize]
        public IActionResult Usercolumn()
        {

            return View("~/Views/Home/Usercolumn.cshtml");
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
