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
        [AllowAnonymous] // 建議標註，確保未登入也能打這個 Action
        public async Task<IActionResult> Login(string username, string password)
        {
            bool isAjax =
                string.Equals(Request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase) ||
                (Request.Headers["Accept"].ToString()?.IndexOf("application/json", StringComparison.OrdinalIgnoreCase) ?? -1) >= 0;

            // 基本清理
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

            // ====== 登入成功：動態檢查頭像檔是否存在 ======
            var userIdStr = result.UserId.ToString();
            var imagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "image", $"{userIdStr}.png");
            bool hasAvatar = System.IO.File.Exists(imagePath);

            // 順便提供前端可直接用的 AvatarUrl（若沒有就給預設）
            string avatarUrl = hasAvatar ? $"/image/{userIdStr}.png" : "/image/default.png";

            // 登入成功：建立 Claims
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, result.UserId.ToString()), // 依你的回傳調整
                new Claim(ClaimTypes.Name, username),
                new Claim("avatarUrl", avatarUrl) // 方便前端直接顯示
                // new Claim("LoginProvider", "Local") // 若要判斷登入來源可加
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

            // 把想帶進 callback 的資訊放在 Items 裡
            props.Items["returnUrl"] = returnUrl;
            props.Items["scheme"] = provider;

            return Challenge(props, provider);
        }

        // 我們自己的 Callback（不是給 Google 的）
        // Google 先回 /signin-google -> 中介軟體處理後再導到這裡
        [HttpGet]
        [AllowAnonymous]
        [Route("externallogin-callback")]
        public async Task<IActionResult> ExternalLoginCallback(string returnUrl = "/Home/Index")
        {
            // 從外部登入 cookie 取回使用者（這裡才拿得到外部 claims）
            var extAuth = await HttpContext.AuthenticateAsync("External");
            // 此時使用者已經由 Cookie Scheme 登入（Program.cs 設了 DefaultScheme = Cookies）
            // 不檢查 User.Identity.IsAuthenticated（此時還沒簽你自己的 Cookies）
            // 應該檢查 extAuth.Succeeded / extAuth.Principal
            if (!extAuth.Succeeded || extAuth.Principal == null)
            {
                // 失敗：印出 Failure 訊息
                var reason = extAuth.Failure?.Message ?? "未知原因";
                TempData["AlertMessage"] = "外部登入失敗：" + reason;
                return RedirectToAction("Login");
            }
            // 取外部提供的資訊
            var extUser = extAuth.Principal!;
            var provider = extAuth.Properties?.Items.TryGetValue("scheme", out var s) 
                == true ? s : "External";
            var nameId = extUser.FindFirst(ClaimTypes.NameIdentifier)?.Value        // OIDC 會對應到 sub
                           ?? extUser.FindFirst("sub")?.Value;
            var name = extUser.FindFirst(ClaimTypes.Name)?.Value
                           ?? extUser.Identity?.Name
                           ?? "User";
            var email = extUser.FindFirst(ClaimTypes.Email)?.Value;
            // 外部供應商的唯一識別（字串，可能很長）
            var externalKey =
                extUser.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                extUser.FindFirst("sub")?.Value ??
                throw new Exception("外部ID缺失");
            // ★ 取得「本地整數」UserId（沒有就建立）
            var db = new DBmanager(); // 你現有的資料層
            int localUid = db.GetOrCreateLocalUserIdByExternal(provider, externalKey, name, email);
            // ====== 登入成功：動態檢查頭像檔是否存在 ======
            // 檔名/頭像一律用本地 uid
            var imagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "image", $"{localUid}.png");
            bool hasAvatar = System.IO.File.Exists(imagePath);
            // 順便提供前端可直接用的 AvatarUrl（若沒有就給預設）
            string avatarUrl = hasAvatar ? $"/image/{localUid}.png" : "/image/M.png";

            // 建立你自己的應用程式 Cookie（之後用 User 讀取）
            var appClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, localUid.ToString()),
                new Claim(ClaimTypes.Name, name),
                new Claim(ClaimTypes.Email, email ?? string.Empty),
                // 另外保存外部資訊（不要用來當主鍵）
                new Claim("ExternalProvider", provider),
                new Claim("ExternalId", externalKey),
                new Claim("avatarUrl", avatarUrl)
            };
            var identity = new ClaimsIdentity(appClaims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            // 清掉外部暫時性 cookie
            await HttpContext.SignOutAsync("External");

            // 防止開放式重導，只允許站內 URL
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
                return Json(new { success = false, message = "沒有歌曲" });

            return Json(new { success = true, songs = songs });
        }
        [HttpGet]
        public IActionResult GetAllSongs()
        {
            var db = new DBmanager();
            var songs = db.GetAllSongs(); // 每筆含 SongID/Title/Artist

            if (songs == null || songs.Count == 0)
                return Json(new { success = false, message = "沒有歌曲" });

            var dto = songs.Select(s => new { songID = s.SongID, title = s.Title, artist = s.Artist }).ToList();
            return Json(new { success = true, songs = dto });
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddToPlaylist(int songId, int? userId) // userId 可省略，亦可從 Claims 取
        {
            try
            {
                // Claims 為主，避免前端竄改
                var uidStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(uidStr)) return Json(new { success = false, message = "未登入" });
                int uid = int.Parse(uidStr);

                var db = new DBmanager();
                var affected = db.NewCollectIfNotExists(uid, songId);
                if (affected == 0)
                    return Json(new { success = true, message = "已在清單中" });

                return Json(new { success = true, message = "已加入" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "系統錯誤：" + ex.Message });
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
            // 判斷是否為 Ajax 請求
            bool isAjax =
            string.Equals(Request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase) ||
            (Request.Headers["Accept"].ToString()?.IndexOf("application/json", StringComparison.OrdinalIgnoreCase) ?? -1) >= 0;
            // 檔案是否為空
            if (file == null || file.FormFile == null || file.FormFile.Length == 0)
            {
            if (isAjax)
                return Json(new { success = false, message = "未選擇檔案" });
                ViewBag.Message = "請選擇檔案";
                return View("~/Views/Home/Usercolumn.cshtml");
            }
            try
            {
                // 檔案資訊
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "image");
                // 確保目錄存在
                if (!Directory.Exists(uploadPath))
                {
                    Directory.CreateDirectory(uploadPath);
                }
                // 檢查檔案類型
                var ext = Path.GetExtension(file.FormFile.FileName).ToLower();
                var allowedExts = new[] {".png" };
                if (!allowedExts.Contains(ext))
                {
                    if (isAjax)
                        return Json(new { success = false, message = "只允許上傳 .png 檔案" });

                    ViewBag.Message = "只允許上傳 .png 檔案";
                    return View("~/Views/Home/Usercolumn.cshtml");
                }
                // 儲存檔案
                var fileName = $"{userId}{ext}";
                var filePath = Path.Combine(uploadPath, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.FormFile.CopyToAsync(stream);
                }
                // ===== 更新登入 cookie 的 avatarUrl =====
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
                    return Json(new { success = true, message = "上傳成功！", fileName = fileName });
                ViewBag.Message = "上傳成功！";
                return View("~/Views/Home/Usercolumn.cshtml");
            }
            catch (Exception ex)
            {
                if (isAjax)
                    return Json(new { success = false, message = "系統錯誤：" + ex.Message });

                ViewBag.Message = "系統錯誤：" + ex.Message;
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
