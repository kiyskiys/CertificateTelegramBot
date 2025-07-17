using Microsoft.EntityFrameworkCore;


namespace CertificateTelegramBot_Main.Data
{
    public class ApplicationDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Certificate> Certificates { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Data source = sertificate_tg_bot.db");
        }
    }
}
