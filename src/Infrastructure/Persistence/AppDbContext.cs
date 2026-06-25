using EntityFramework.Exceptions.SqlServer;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Data;

using Core.Entities;

namespace Infrastructure.Persistence;

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
            entity.Property(e => e.FirstName).HasColumnName("firstName");
            entity.Property(e => e.LastName).HasColumnName("lastName");
            entity.Property(e => e.Email).HasColumnName("email");
            entity.Property(e => e.Mobile).HasColumnName("mobile");
            entity.Property(e => e.Password).HasColumnName("password");
            entity.Property(e => e.IsActive).HasColumnName("isActive");
            entity.Property(e => e.IsDeleted).HasColumnName("isDeleted");
            entity.Property(e => e.CreatedBy).HasColumnName("createdBy");
            entity.Property(e => e.CreatedAt).HasColumnName("createdAt");
            entity.Property(e => e.UpdatedBy).HasColumnName("updatedBy");
            entity.Property(e => e.UpdatedAt).HasColumnName("updatedAt");
        });
        modelBuilder.Entity<ChatThread>(entity =>
        {
            entity.ToTable("ChatThread", "dbo");
            entity.Property(e => e.ChatAgent).HasColumnName("chatAgent");
        });
        modelBuilder.Entity<ChatMessage>(entity =>
        {
            entity.ToTable("ChatMessage", "dbo");
            entity.Property(e => e.Role).HasColumnName("role");
        });
        modelBuilder.Entity<UserSetting>(entity =>
        {
            entity.ToTable("UserSetting", "dbo");
            entity.Property(e => e.UserId).HasColumnName("userId");
            entity.Property(e => e.IsActive).HasColumnName("isActive");
            entity.Property(e => e.IsDeleted).HasColumnName("isDeleted");
            entity.Property(e => e.CreatedBy).HasColumnName("createdBy");
            entity.Property(e => e.CreatedAt).HasColumnName("createdAt");
            entity.Property(e => e.UpdatedBy).HasColumnName("updatedBy");
            entity.Property(e => e.UpdatedAt).HasColumnName("updatedAt");
            entity.Property(e => e.EmailSettingsJson).HasColumnName("EmailSettingsJson");
            entity.HasIndex(e => e.UserId).IsUnique();
        });
    }

    #endregion

    #region # Helpers

    public IDbConnection Connection { get; private set; }

    #endregion

    public DbSet<User> Users { get; set; }
    public DbSet<ChatThread> ChatThreads { get; set; }
    public DbSet<ChatMessage> ChatMessages { get; set; }
    public DbSet<UserSetting> UserSettings { get; set; }
}
