using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.DataAnnotations;
using MusicPlayer.Models;
using MVC_DB_.Models;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Reflection;
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
        [AllowAnonymous] // ��ĳ�е��A�T�O���n�J�]�ॴ�o�� Action
        public async Task<IActionResult> Login(string username, string password)
        {
            bool isAjax =
                string.Equals(Request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase) ||
                (Request.Headers["Accept"].ToString()?.IndexOf("application/json", StringComparison.OrdinalIgnoreCase) ?? -1) >= 0;

            // �򥻲M�z
            username = username?.Trim();
            password = password?.Trim();

            var svc = new Logincheck();
            var result = svc.Login(username, password);

            if (!result.Success)
            {
                if (isAjax) return Json(new { success = false, message = result.Message });
                ViewBag.Message = result.Message;
                return View("~/Views/Login/Login.cshtml");
            }

            // ====== �n�J���\�G�ʺA�ˬd�Y���ɬO�_�s�b ======
            var userIdStr = result.UserId.ToString();
            var imagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "image", $"{userIdStr}.png");
            bool hasAvatar = System.IO.File.Exists(imagePath);

            // ���K���ѫe�ݥi�����Ϊ� AvatarUrl�]�Y�S���N���w�]�^
            string avatarUrl = hasAvatar ? $"/image/{userIdStr}.png" : "/image/default.png";

            // �n�J���\�G�إ� Claims
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, result.UserId.ToString()), // �̧A���^�ǽվ�
                new Claim(ClaimTypes.Name, username),
                new Claim("avatarUrl", avatarUrl) // ��K�e�ݪ������
                // new Claim("LoginProvider", "Local") // �Y�n�P�_�n�J�ӷ��i�[
            };
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme); // "Cookies"
            var principal = new ClaimsPrincipal(identity);

            // �g�J���� Cookie
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme, // �����P���U�� scheme �@�P
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = true,                                  // �Y���u�O��ڡv�A�ΰѼƱ���
                    ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
                });

            // �^��
            if (isAjax)
                return Json(new { success = true, redirectUrl = Url.Action("Index", "Home") });

            // �קK����y����歫�e
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

            // ��Q�a�i callback ����T��b Items ��
            props.Items["returnUrl"] = returnUrl;
            props.Items["scheme"] = provider;

            return Challenge(props, provider);
        }

        // �ڭ̦ۤv�� Callback�]���O�� Google ���^
        // Google ���^ /signin-google -> �����n��B�z��A�ɨ�o��
        [HttpGet]
        [AllowAnonymous]
        [Route("externallogin-callback")]
        public async Task<IActionResult> ExternalLoginCallback(string returnUrl = "/Home/Index")
        {
            // �q�~���n�J cookie ���^�ϥΪ̡]�o�̤~���o��~�� claims�^
            var extAuth = await HttpContext.AuthenticateAsync("External");
            // ���ɨϥΪ̤w�g�� Cookie Scheme �n�J�]Program.cs �]�F DefaultScheme = Cookies�^
            // ���ˬd User.Identity.IsAuthenticated�]�����٨Sñ�A�ۤv�� Cookies�^
            // �����ˬd extAuth.Succeeded / extAuth.Principal
            if (!extAuth.Succeeded || extAuth.Principal == null)
            {
                // ���ѡG�L�X Failure �T��
                var reason = extAuth.Failure?.Message ?? "������]";
                TempData["AlertMessage"] = "�~���n�J���ѡG" + reason;
                return RedirectToAction("Login");
            }
            // ���~�����Ѫ���T
            var extUser = extAuth.Principal!;
            var provider = extAuth.Properties?.Items.TryGetValue("scheme", out var s) 
                == true ? s : "External";
            var nameId = extUser.FindFirst(ClaimTypes.NameIdentifier)?.Value        // OIDC �|������ sub
                           ?? extUser.FindFirst("sub")?.Value;
            var name = extUser.FindFirst(ClaimTypes.Name)?.Value
                           ?? extUser.Identity?.Name
                           ?? "User";
            var email = extUser.FindFirst(ClaimTypes.Email)?.Value;
            // �~�������Ӫ��ߤ@�ѧO�]�r��A�i��ܪ��^
            var externalKey =
                extUser.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                extUser.FindFirst("sub")?.Value ??
                throw new Exception("�~��ID�ʥ�");
            // �� ���o�u���a��ơvUserId�]�S���N�إߡ^
            var db = new DBmanager(); // �A�{������Ƽh
            int localUid = db.GetOrCreateLocalUserIdByExternal(provider, externalKey, name, email);
            // ====== �n�J���\�G�ʺA�ˬd�Y���ɬO�_�s�b ======
            // �ɦW/�Y���@�ߥΥ��a uid
            var imagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "image", $"{localUid}.png");
            bool hasAvatar = System.IO.File.Exists(imagePath);
            // ���K���ѫe�ݥi�����Ϊ� AvatarUrl�]�Y�S���N���w�]�^
            string avatarUrl = hasAvatar ? $"/image/{localUid}.png" : "/image/M.png";

            // �إߧA�ۤv�����ε{�� Cookie�]����� User Ū���^
            var appClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, localUid.ToString()),
                new Claim(ClaimTypes.Name, name),
                new Claim(ClaimTypes.Email, email ?? string.Empty),
                // �t�~�O�s�~����T�]���n�Ψӷ�D��^
                new Claim("ExternalProvider", provider),
                new Claim("ExternalId", externalKey),
                new Claim("avatarUrl", avatarUrl)
            };
            var identity = new ClaimsIdentity(appClaims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            // �M���~���Ȯɩ� cookie
            await HttpContext.SignOutAsync("External");

            // ����}�񦡭��ɡA�u���\���� URL
            if (!Url.IsLocalUrl(returnUrl)) returnUrl = Url.Action("Index", "Home")!;
            return LocalRedirect(returnUrl);
        }

        [Authorize]
        public IActionResult Usercolumn()
        {
            Userlist userlist = new Userlist();
            userlist.getList(int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value));
            return View("~/Views/Home/Usercolumn.cshtml");
        }
        [HttpGet]
        public IActionResult GetSongs(int userId)
        {
            var db = new DBmanager();
            var songs = db.GetSongsByUser(userId);

            if (songs == null || songs.Count == 0)
                return Json(new { success = false, message = "�S���q��" });

            return Json(new { success = true, songs = songs });
        }
        [HttpGet]
        public IActionResult GetAllSongs()
        {
            var db = new DBmanager();
            var songs = db.GetAllSongs(); // �C���t SongID/Title/Artist

            if (songs == null || songs.Count == 0)
                return Json(new { success = false, message = "�S���q��" });

            var dto = songs.Select(s => new { songID = s.SongID, title = s.Title, artist = s.Artist }).ToList();
            return Json(new { success = true, songs = dto });
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddToPlaylist(int songId, int? userId) // userId �i�ٲ��A��i�q Claims ��
        {
            try
            {
                // Claims ���D�A�קK�e��«��
                var uidStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(uidStr)) return Json(new { success = false, message = "���n�J" });
                int uid = int.Parse(uidStr);

                var db = new DBmanager();
                var affected = db.NewCollectIfNotExists(uid, songId);
                if (affected == 0)
                    return Json(new { success = true, message = "�w�b�M�椤" });

                return Json(new { success = true, message = "�w�[�J" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "�t�ο��~�G" + ex.Message });
            }
        }
        [HttpPost]
        public IActionResult RemoveFromPlaylist(int songId, int userId)
        {
            try
            {
                var db = new DBmanager();
                db.RemoveFromPlaylist(userId, songId);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> UploadFile(Fileupload file)
        {
            // �P�_�O�_�� Ajax �ШD
            bool isAjax =
            string.Equals(Request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase) ||
            (Request.Headers["Accept"].ToString()?.IndexOf("application/json", StringComparison.OrdinalIgnoreCase) ?? -1) >= 0;
            // �ɮ׬O�_����
            if (file == null || file.FormFile == null || file.FormFile.Length == 0)
            {
            if (isAjax)
                return Json(new { success = false, message = "������ɮ�" });
                ViewBag.Message = "�п���ɮ�";
                return View("~/Views/Home/Usercolumn.cshtml");
            }
            try
            {
                // �ɮ׸�T
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "image");
                // �T�O�ؿ��s�b
                if (!Directory.Exists(uploadPath))
                {
                    Directory.CreateDirectory(uploadPath);
                }
                // �ˬd�ɮ�����
                var ext = Path.GetExtension(file.FormFile.FileName).ToLower();
                var allowedExts = new[] {".png" };
                if (!allowedExts.Contains(ext))
                {
                    if (isAjax)
                        return Json(new { success = false, message = "�u���\�W�� .png �ɮ�" });

                    ViewBag.Message = "�u���\�W�� .png �ɮ�";
                    return View("~/Views/Home/Usercolumn.cshtml");
                }
                // �x�s�ɮ�
                var fileName = $"{userId}{ext}";
                var filePath = Path.Combine(uploadPath, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.FormFile.CopyToAsync(stream);
                }
                // ===== ��s�n�J cookie �� avatarUrl =====
                var user = User as ClaimsPrincipal;
                if (user != null)
                {
                    var identity = user.Identity as ClaimsIdentity;
                    if (identity != null)
                    {
                        var oldAvatar = identity.FindFirst("avatarUrl");
                        if (oldAvatar != null)
                            identity.RemoveClaim(oldAvatar);

                        var newAvatarUrl = $"/image/{userId}.png";
                        identity.AddClaim(new Claim("avatarUrl", newAvatarUrl));

                        await HttpContext.SignInAsync(
                            CookieAuthenticationDefaults.AuthenticationScheme,
                            new ClaimsPrincipal(identity),
                            new AuthenticationProperties
                            {
                                IsPersistent = true,
                                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
                            });
                    }
                }
                if (isAjax)
                    return Json(new { success = true, message = "�W�Ǧ��\�I", fileName = fileName });
                ViewBag.Message = "�W�Ǧ��\�I";
                return View("~/Views/Home/Usercolumn.cshtml");
            }
            catch (Exception ex)
            {
                if (isAjax)
                    return Json(new { success = false, message = "�t�ο��~�G" + ex.Message });

                ViewBag.Message = "�t�ο��~�G" + ex.Message;
                return View("~/Views/Home/Usercolumn.cshtml");
            }
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
