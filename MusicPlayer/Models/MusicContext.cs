using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace MusicPlayer.Models
{
    public class MusicContext : DbContext
    {
        public MusicContext(DbContextOptions<MusicContext> options) : base(options) { }

        public DbSet<SongList> SongList { get; set; }
    }
}