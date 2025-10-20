using System.ComponentModel.DataAnnotations;

namespace MusicPlayer.Models
{
    public class SongList
    {
        [Key]
        public int SongID { get; set; }
        public string title { get; set; }
        public string artist { get; set; }
        public string file_path { get; set; }
        public DateTime download_date { get; set; }
    }
}
