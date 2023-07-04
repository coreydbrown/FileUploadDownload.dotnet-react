using FileApi.Models;
using Microsoft.EntityFrameworkCore;

namespace FileApi.Data;

public partial class FileApiDbContext : DbContext
{
    public FileApiDbContext()
    {
    }

    public FileApiDbContext(DbContextOptions<FileApiDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<FileModel> Files { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseSqlServer("Name=ConnectionStrings:FileApiDbConnectionString");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FileModel>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Files__3214EC07D45CAC7C");

            entity.Property(e => e.ContentType).HasMaxLength(255);
            entity.Property(e => e.Name).HasMaxLength(255);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
