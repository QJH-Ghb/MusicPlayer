using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.DataAnnotations;
using MusicPlayer.Models;
using MVC_DB_.Models;

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
        public IActionResult Login(string username,string password)
        {
            Logincheck logincheck = new Logincheck();
            bool result = logincheck.ValidateUser(username, password);
            ViewBag.Message = "�п�J�b���αK�X";
            if (result)
            {

                // �n�J���\
                return View("~/Views/Home/Index.cshtml");
            }
            else
            {
                // �n�J����
                ViewBag.Message = "�b���αK�X���~";
                return View("~/Views/Login/Login.cshtml");
            }
        }
        [HttpPost]
        public IActionResult Register(string username, string password,string email)
        {
            DBmanager db = new DBmanager();
            if (db.CheckAccountExists(username))
            {
                TempData["AlertMessage"] = "���b���w�Q���U�I";
                return View("~/Views/Login/Login.cshtml");
            }
            else
            {
                if (string.IsNullOrWhiteSpace(username) ||
                string.IsNullOrWhiteSpace(password) ||
                string.IsNullOrWhiteSpace(email)
                )
                {
                    ViewBag.Message = "�Ҧ����Ҭ�����";
                    return View("~/Views/Login/Login.cshtml");
                }
                db.newAccount(new account { userName = username, passWord = password, email = email });
                ViewBag.Message = "���U���\�I";
            }
            return View("~/Views/Home/Index.cshtml");
        }
        public IActionResult Search()
        {
            return View();
        }
        public IActionResult Profile()
        {
            return View();
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
