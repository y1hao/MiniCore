using Microsoft.EntityFrameworkCore;
using MiniCore.Web.Models;

namespace MiniCore.Web.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<ShortLink> ShortLinks { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ShortLink>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ShortCode).IsUnique();
            entity.Property(e => e.ShortCode).HasMaxLength(20).IsRequired();
            entity.Property(e => e.OriginalUrl).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
        });
    }
}

