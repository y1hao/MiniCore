using MiniCore.Framework.Data;
using MiniCore.Web.Models;

namespace MiniCore.Web.Data;

public class AppDbContext : DbContext
{
    private DbSet<ShortLink>? _shortLinks;

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<ShortLink> ShortLinks
    {
        get
        {
            _shortLinks ??= new DbSet<ShortLink>(this, GetTableName(typeof(ShortLink)));
            return _shortLinks;
        }
        set => _shortLinks = value;
    }

    protected override string GetTableName(Type entityType)
    {
        // Use pluralized table name
        if (entityType == typeof(ShortLink))
        {
            return "ShortLinks";
        }
        return base.GetTableName(entityType);
    }
}
