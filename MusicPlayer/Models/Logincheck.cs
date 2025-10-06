using MVC_DB_.Models;
using System.Collections.Generic;
using System.Linq;

namespace MusicPlayer.Models
{
    public class Logincheck
    {
        public string username { get; set; }
        public string password { get; set; }

        // 驗證帳號密碼的方法
        public bool ValidateUser(string inputUser, string inputPass)
        {
            DBmanager dbmanager = new DBmanager();
            List<account> accounts = dbmanager.getAccounts();

            // LINQ 查詢是否有匹配的帳號
            var user = accounts.FirstOrDefault(a => a.userName == inputUser && a.passWord == inputPass);
            return user != null;
        }
    }
}
