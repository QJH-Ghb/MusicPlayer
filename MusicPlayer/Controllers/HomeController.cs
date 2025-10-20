using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.DataAnnotations;
using Microsoft.Data.SqlClient;
using MusicPlayer.Models;
using MVC_DB_.Models;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;


namespace MusicPlayer.Controllers
{
    public class HomeController : Controller
    {
        private readonly string _connStr = @"Server=(localdb)\MSSQLLocalDB;Database=MusicLibrary;Trusted_Connection=True;";
        private readonly MusicContext _context;

        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger, MusicContext context)
        {
            _logger = logger;
            _context = context;

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
            var musicFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "music");

            var files = Directory.Exists(musicFolder)
                        ? Directory.GetFiles(musicFolder, "*.mp3")
                        : new string[0];

            var songs = new Dictionary<string, string>();

            foreach (var file in files)
            {
                var fileName = Path.GetFileName(file);       // �ɦW
                var relativePath = "/music/" + fileName;     // �e�ݥi�Ϊ��۹���|
                songs[fileName] = relativePath;
            }

            Console.WriteLine("Ū�쪺�q���ƶq: " + songs.Count);

            ViewData["Songs"] = songs;
            return View();
        }
        public IActionResult PlayList()
        {
            var songs = _context.SongList.ToList();

            return View(songs);
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
