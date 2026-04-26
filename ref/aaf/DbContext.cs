using AAF.Models;

using EntityFramework.Exceptions.SqlServer;

using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

using System.Data;

namespace AAF.Data;

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
            //Connection.Open();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Could not open the database connection.", ex);
        }
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
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
        //base.OnModelCreating(modelBuilder);

        //modelBuilder.Entity<Family>()
        //    .HasMany(f => f.FamilyMembers)
        //    .WithOne(m => m.Family)
        //    .HasForeignKey(m => m.FamilyId);
    }

    #endregion

    #region # Helpers

    public IDbConnection Connection { get; private set; }

    #endregion

    public DbSet<Campaign> Campaigns { get; set; }
    public DbSet<Models.Program> Programs { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Family> Families { get; set; }
    public DbSet<FamilyMember> FamilyMembers { get; set; }
    public DbSet<Donor> Donors { get; set; }
    public DbSet<LateRegistration> LateRegistrations { get; set; }
}
