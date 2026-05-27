using EntityFramework.Exceptions.SqlServer;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;

using WebApp.Models;

namespace WebApp.Data;

public class AppDbContext : DbContext
{
    #region # Init

    private readonly IConfiguration _configuration;

    public AppDbContext(DbContextOptions<AppDbContext> options, IConfiguration configuration)
        : base(options)
    {
        _configuration = configuration;
        try
        {
            var conn = _configuration.GetConnectionString("DefaultConnection");
            Connection = new SqlConnection(conn);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Could not open the database connection.", ex);
        }
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        ArgumentNullException.ThrowIfNull(optionsBuilder);
        try
        {
            if (!optionsBuilder.IsConfigured)
            {
                var conn = _configuration.GetConnectionString("DefaultConnection");
                optionsBuilder.UseSqlServer(conn);
            }

            optionsBuilder.EnableDetailedErrors(true);
            optionsBuilder.EnableSensitiveDataLogging(true);

            optionsBuilder.UseLazyLoadingProxies(false);
            optionsBuilder.UseExceptionProcessor();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Could not open the database connection.", ex);
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("User", "dbo");
        });
        modelBuilder.Entity<ChatThread>(entity =>
        {
            entity.ToTable("ChatThread", "dbo");
        });
        modelBuilder.Entity<ChatMessage>(entity =>
        {
            entity.ToTable("ChatMessage", "dbo");
        });
    }

    #endregion

    #region # Helpers

    public IDbConnection Connection { get; private set; }

    #endregion

    public DbSet<User> Users { get; set; }
    public DbSet<ChatThread> ChatThreads { get; set; }
    public DbSet<ChatMessage> ChatMessages { get; set; }
}
