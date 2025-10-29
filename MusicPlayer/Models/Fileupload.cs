using MVC_DB_.Models;

namespace MusicPlayer.Models
{
    public class Fileupload : account
    {
        public IFormFile FormFile { set; get; }
    }
}
