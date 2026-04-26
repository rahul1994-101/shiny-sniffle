using EntityFramework.Exceptions.SqlServer;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;
using WebUI.Models;

namespace WebUI.Utilities.Helpers;

public class TenantDbContext : DbContext
{
    #region # Init

    private readonly IConfiguration _configuration;

    public TenantDbContext(DbContextOptions<TenantDbContext> options, IConfiguration configuration)
        : base(options)
    {
        _configuration = configuration;
        try
        {
            var conn = _configuration.GetConnectionString("TenantDB");
            Connection = new SqlConnection(conn);
            //Connection.Open();
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
                var conn = _configuration.GetConnectionString("TenantDB");
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

        // Configure User entity table and column mappings
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("User", "dbo");
            
            // Map properties to database columns (camelCase in DB, PascalCase in C#)
            //entity.Property(e => e.Id).HasColumnName("id");
            //entity.Property(e => e.FirstName).HasColumnName("firstName");
            //entity.Property(e => e.LastName).HasColumnName("lastName");
            //entity.Property(e => e.Email).HasColumnName("email");
            //entity.Property(e => e.Mobile).HasColumnName("mobile");
            //entity.Property(e => e.Password).HasColumnName("password");
            //entity.Property(e => e.Role).HasColumnName("role");
            //entity.Property(e => e.IsActive).HasColumnName("isActive");
            //entity.Property(e => e.IsDeleted).HasColumnName("isDeleted");
            //entity.Property(e => e.CreatedBy).HasColumnName("createdBy");
            //entity.Property(e => e.CreatedOn).HasColumnName("createdAt");
            //entity.Property(e => e.UpdatedBy).HasColumnName("updatedBy");
            //entity.Property(e => e.UpdatedOn).HasColumnName("updatedAt");
        });

        //modelBuilder.Entity<Family>()
        //    .HasMany(f => f.FamilyMembers)
        //    .WithOne(m => m.Family)
        //    .HasForeignKey(m => m.FamilyId);
    }

    #endregion

    #region # Helpers

    public IDbConnection Connection { get; private set; }

    #endregion

    public DbSet<User> Users { get; set; }
}
