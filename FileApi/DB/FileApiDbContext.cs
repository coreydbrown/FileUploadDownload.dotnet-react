using FileApi.Models;
using Microsoft.EntityFrameworkCore;

namespace FileApi.DB
{
    public class FileApiDbContext : DbContext
    {
        public FileApiDbContext(DbContextOptions options) : base(options)
        {
        }

        public DbSet<FileModel> Files { get; set; }
    }
}
