using MVC_DB_.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace MusicPlayer.Models
{
    /// <summary>
    /// 封裝登入/註冊邏輯的服務類
    /// <summary>
    public class Logincheck
    {
        /// <summary>
        /// 登入流程（會做欄位檢查 + 雜湊比對）
        /// <summary>
        public AuthResult Login(string inputUser, string inputPass)
        {
            // 必填驗證
            if (string.IsNullOrWhiteSpace(inputUser) || string.IsNullOrWhiteSpace(inputPass))
                return AuthResult.Fail("請輸入帳號與密碼");

            try
            {
                // 取得帳號清單
                var dbmanager = new DBmanager();
                List<account> accounts = dbmanager.getAccounts();
                if (accounts == null || accounts.Count == 0)
                    return AuthResult.Fail("帳號或密碼錯誤");

                // 使用與註冊相同的雜湊方式
                string hashedInput = HashPassword(inputPass);

                // 比對（帳號不分大小寫）
                var user = accounts.FirstOrDefault(a =>
                    a.userName.Equals(inputUser, StringComparison.OrdinalIgnoreCase) &&
                    a.passWord == hashedInput);

                if (user == null)
                    return AuthResult.Fail("帳號或密碼錯誤");

                // 成功，並將該使用者資訊全部打入cookie的token
                return AuthResult.Ok(
                    redirectUrl: "/Home/Index",
                    userId: user.userID,
                    userName: user.userName
                    );
            }
            catch (Exception ex)
            {
                return AuthResult.Fail("系統錯誤：" + ex.Message);
            }
        }

        /// <summary>
        /// 註冊流程（欄位檢查 → Email 格式 → 密碼強度 → 重複帳號 → 寫入）
        /// </summary>
        public AuthResult Register(string inputUser, string inputPass, string inputEmail)
        {
            string username = inputUser?.Trim();
            string password = inputPass?.Trim();
            string email = inputEmail?.Trim();

            // 必填驗證
            if (string.IsNullOrWhiteSpace(username) ||
                string.IsNullOrWhiteSpace(password) ||
                string.IsNullOrWhiteSpace(email))
                return AuthResult.Fail("所有欄位皆為必填");

            // Email 格式
            if (!Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                return AuthResult.Fail("Email 格式不正確");

            // 密碼強度：至少 6 碼，含英文字母與數字
            if (password.Length < 6 || !Regex.IsMatch(password, @"[A-Za-z]") || !Regex.IsMatch(password, @"\d"))
                return AuthResult.Fail("密碼需至少 6 碼，且包含英文字母與數字");

            try
            {
                var db = new DBmanager();

                // 檢查帳號重複（不分大小寫）
                if (db.CheckAccountExists(username))
                    return AuthResult.Fail("此帳號已被註冊！");

                // 雜湊密碼（與登入一致）
                string hashedPassword = HashPassword(password);

                db.newAccount(new account
                {
                    userName = username,
                    passWord = hashedPassword,
                    email = email
                });

                return AuthResult.Ok(message: "註冊成功！請登入", redirectUrl: "/Home/Login");
            }
            catch (Exception ex)
            {
                return AuthResult.Fail("系統錯誤：" + ex.Message);
            }
        }

        // —— 供內部使用的工具方法 —— //

        /// <summary>
        /// 與註冊/登入一致的 SHA256 雜湊（未加鹽；建議之後改 BCrypt）
        /// </summary>
        private string HashPassword(string password)
        {
            using (var sha = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(password ?? string.Empty);
                var hash = sha.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }
    }

    /// <summary>
    /// 封裝驗證結果（讓 Controller 好處理 AJAX/一般表單）
    /// </summary>
    public class AuthResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string RedirectUrl { get; set; }
        public int? UserId { get; set; }
        public string? UserName { get; set; }

        public static AuthResult Ok(string message = null, string redirectUrl = null,int ? userId = null, string userName = null)
            => new AuthResult { Success = true, Message = message, RedirectUrl = redirectUrl, UserId = userId, UserName = userName };

        public static AuthResult Fail(string message)
            => new AuthResult { Success = false, Message = message };
    }
}
