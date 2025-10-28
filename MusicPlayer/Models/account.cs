using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MVC_DB_.Models
{
    public class account:DBmanager
    {
        public int userID { get; set; }
        public string userName { get; set; }
        public string passWord { get; set; }
        public string email { get; set; }
        public DateTime createTime { get; set; }
    }

}
