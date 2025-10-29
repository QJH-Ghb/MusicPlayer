using Microsoft.Data.SqlClient;
using MVC_DB_.Models;

namespace MusicPlayer.Models
{
    public class Userlist
    {
        public HashSet<int>? getList(int userID)
        {
            var dbmanager = new DBmanager();
            Dictionary<int, List<int>> playList = dbmanager.getPlaylists();
            if (playList.TryGetValue(userID, out var songs))
                return new HashSet<int>(songs);
            return null;
        }
        
    }
}
