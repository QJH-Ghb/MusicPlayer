using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.DataAnnotations;
using MusicPlayer.Models;
using MVC_DB_.Models;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
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
        public IActionResult Login(string username, string password)
        {
            bool isAjax =
                string.Equals(Request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase) ||
                (Request.Headers["Accept"].ToString()?.IndexOf("application/json", StringComparison.OrdinalIgnoreCase) ?? -1) >= 0;

            var svc = new Logincheck();
            var result = svc.Login(username, password);

            if (isAjax)
            {
                if (result.Success)
                    return Json(new { success = true, redirectUrl = Url.Action("Index", "Home") });
                return Json(new { success = false, message = result.Message });
            }

            if (result.Success)
                return View("~/Views/Home/Index.cshtml");

            ViewBag.Message = result.Message;
            return View("~/Views/Login/Login.cshtml");
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
                TempData["AlertMessage"] = result.Message ?? "���U���\�I�еn�J";
                return RedirectToAction("Login", "Home");
            }

            ViewBag.Message = result.Message;
            return View("~/Views/Login/Login.cshtml");
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ExternalLogin(string provider, string returnUrl = "/Home/Index")
        {
            // Google ������A�|���^ /signin-google�]�Ѥ����n��B�z�^
            // �B�z���A��ϥΪ� redirect ��o�̳]�w�� RedirectUri�]�Y�ڭ̦ۤv�� Callback�^
            var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Home", new { returnUrl });
            var props = new AuthenticationProperties { RedirectUri = redirectUrl };
            return Challenge(props, provider);
        }

        // �ڭ̦ۤv�� Callback�]���O�� Google ���^
        // Google ���^ /signin-google -> �����n��B�z��A�ɨ�o��
        [HttpGet]
        [AllowAnonymous]
        [Route("externallogin-callback")]
        public async Task<IActionResult> ExternalLoginCallback(string returnUrl = "/Home/Index")
        {
            // ���ɨϥΪ̤w�g�� Cookie Scheme �n�J�]Program.cs �]�F DefaultScheme = Cookies�^
            // �A�i�H�q HttpContext.User ���� claims�F���B�ܽd�̤p�ˬd
            if (!(User?.Identity?.IsAuthenticated ?? false))
            {
                TempData["AlertMessage"] = "�~���n�J���ѡA�ЦA�դ@���C";
                return RedirectToAction("Login");
            }

            // �o�̧A�i�i�@�B�G�إ�/�j�w���a�b���]DBmanager�^�� �w�b�e���оǴ��ѽd��
            // ���^�h returnUrl
            return LocalRedirect(returnUrl);
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
