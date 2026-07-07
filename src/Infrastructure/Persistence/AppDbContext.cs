using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users { get; set; }

    public DbSet<ChatThread> ChatThreads { get; set; }

    public DbSet<ChatMessage> ChatMessages { get; set; }

    public DbSet<UserSetting> UserSettings { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("User", "dbo");
            entity.Property(e => e.FirstName).HasColumnName("firstName").HasMaxLength(50);
            entity.Property(e => e.LastName).HasColumnName("lastName").HasMaxLength(50);
            entity.Property(e => e.Email).HasColumnName("email").HasMaxLength(255);
            entity.Property(e => e.Mobile).HasColumnName("mobile").HasMaxLength(20);
            entity.Property(e => e.Password).HasColumnName("password").HasMaxLength(512);
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
            entity.Property(e => e.UserId).HasColumnName("userId");
            entity.Property(e => e.Title).HasColumnName("title").HasMaxLength(200);
            entity.Property(e => e.ChatAgent).HasColumnName("chatAgent");
            entity.Property(e => e.MemorySummary).HasColumnName("memorySummary");
            entity.Property(e => e.MemorySummaryThroughMessageId).HasColumnName("memorySummaryThroughMessageId");
            entity.Property(e => e.IsActive).HasColumnName("isActive");
            entity.Property(e => e.IsDeleted).HasColumnName("isDeleted");
            entity.Property(e => e.CreatedBy).HasColumnName("createdBy");
            entity.Property(e => e.CreatedAt).HasColumnName("createdAt");
            entity.Property(e => e.UpdatedBy).HasColumnName("updatedBy");
            entity.Property(e => e.UpdatedAt).HasColumnName("updatedAt");
        });
        modelBuilder.Entity<ChatMessage>(entity =>
        {
            entity.ToTable("ChatMessage", "dbo");
            entity.Property(e => e.ChatThreadId).HasColumnName("chatThreadId");
            entity.Property(e => e.Role).HasColumnName("role").HasMaxLength(20);
            entity.Property(e => e.Content).HasColumnName("content");
            entity.Property(e => e.IsActive).HasColumnName("isActive");
            entity.Property(e => e.IsDeleted).HasColumnName("isDeleted");
            entity.Property(e => e.CreatedBy).HasColumnName("createdBy");
            entity.Property(e => e.CreatedAt).HasColumnName("createdAt");
            entity.Property(e => e.UpdatedBy).HasColumnName("updatedBy");
            entity.Property(e => e.UpdatedAt).HasColumnName("updatedAt");
        });
        modelBuilder.Entity<UserSetting>(entity =>
        {
            entity.ToTable("UserSetting", "dbo");
            entity.Property(e => e.UserId).HasColumnName("userId");
            entity.Property(e => e.EmailSettingsJson).HasColumnName("EmailSettingsJson");
            entity.Property(e => e.IsActive).HasColumnName("isActive");
            entity.Property(e => e.IsDeleted).HasColumnName("isDeleted");
            entity.Property(e => e.CreatedBy).HasColumnName("createdBy");
            entity.Property(e => e.CreatedAt).HasColumnName("createdAt");
            entity.Property(e => e.UpdatedBy).HasColumnName("updatedBy");
            entity.Property(e => e.UpdatedAt).HasColumnName("updatedAt");
            entity.HasIndex(e => e.UserId).IsUnique();
        });
    }
}
