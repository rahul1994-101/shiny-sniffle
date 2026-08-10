using Infrastructure.Persistence.dbo;
using Infrastructure.Persistence.chat;
using Infrastructure.Persistence.workspace;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users { get; set; }

    public DbSet<ChatThread> ChatThreads { get; set; }

    public DbSet<ChatMessage> ChatMessages { get; set; }

    public DbSet<UserSetting> UserSettings { get; set; }

    public DbSet<EmailProvider> EmailProviders { get; set; }

    public DbSet<EmailAccount> EmailAccounts { get; set; }

    public DbSet<Contact> Contacts { get; set; }

    public DbSet<Tag> Tags { get; set; }

    public DbSet<Bucket> Buckets { get; set; }

    public DbSet<TagAssignment> TagAssignments { get; set; }

    public DbSet<BucketMember> BucketMembers { get; set; }

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
            entity.ToTable("ChatThread", "chat");
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
            entity.ToTable("ChatMessage", "chat");
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
            entity.Property(e => e.IsActive).HasColumnName("isActive");
            entity.Property(e => e.IsDeleted).HasColumnName("isDeleted");
            entity.Property(e => e.CreatedBy).HasColumnName("createdBy");
            entity.Property(e => e.CreatedAt).HasColumnName("createdAt");
            entity.Property(e => e.UpdatedBy).HasColumnName("updatedBy");
            entity.Property(e => e.UpdatedAt).HasColumnName("updatedAt");
            entity.HasIndex(e => e.UserId).IsUnique();
        });
        modelBuilder.Entity<EmailProvider>(entity =>
        {
            entity.ToTable("EmailProvider", "dbo");
            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(100);
            entity.Property(e => e.Slug).HasColumnName("slug").HasMaxLength(64);
            entity.Property(e => e.ImapHost).HasColumnName("imapHost").HasMaxLength(255);
            entity.Property(e => e.ImapPort).HasColumnName("imapPort");
            entity.Property(e => e.ImapUseSsl).HasColumnName("imapUseSsl");
            entity.Property(e => e.SmtpHost).HasColumnName("smtpHost").HasMaxLength(255);
            entity.Property(e => e.SmtpPort).HasColumnName("smtpPort");
            entity.Property(e => e.SmtpUseSsl).HasColumnName("smtpUseSsl");
            entity.Property(e => e.SetupHelpUrl).HasColumnName("setupHelpUrl").HasMaxLength(500);
            entity.Property(e => e.SortOrder).HasColumnName("sortOrder");
            entity.Property(e => e.IsSystem).HasColumnName("isSystem");
            entity.Property(e => e.IsActive).HasColumnName("isActive");
            entity.Property(e => e.IsDeleted).HasColumnName("isDeleted");
            entity.Property(e => e.CreatedBy).HasColumnName("createdBy");
            entity.Property(e => e.CreatedAt).HasColumnName("createdAt");
            entity.Property(e => e.UpdatedBy).HasColumnName("updatedBy");
            entity.Property(e => e.UpdatedAt).HasColumnName("updatedAt");
            entity.HasIndex(e => e.Slug)
                .IsUnique()
                .HasFilter("[isDeleted] = 0");
            entity.HasIndex(e => e.SortOrder)
                .HasFilter("[isDeleted] = 0");
        });
        modelBuilder.Entity<EmailAccount>(entity =>
        {
            entity.ToTable("EmailAccount", "workspace");
            entity.Property(e => e.UserId).HasColumnName("userId");
            entity.Property(e => e.EmailProviderId).HasColumnName("emailProviderId");
            entity.Property(e => e.EmailAddress).HasColumnName("emailAddress").HasMaxLength(255);
            entity.Property(e => e.Username).HasColumnName("username").HasMaxLength(255);
            entity.Property(e => e.Password).HasColumnName("password").HasMaxLength(512);
            entity.Property(e => e.IsDefault).HasColumnName("isDefault");
            entity.Property(e => e.SortOrder).HasColumnName("sortOrder");
            entity.Property(e => e.Alias).HasColumnName("alias").HasMaxLength(64).IsRequired();
            entity.Property(e => e.Context).HasColumnName("context").HasMaxLength(2000);
            entity.Property(e => e.IsActive).HasColumnName("isActive");
            entity.Property(e => e.IsDeleted).HasColumnName("isDeleted");
            entity.Property(e => e.CreatedBy).HasColumnName("createdBy");
            entity.Property(e => e.CreatedAt).HasColumnName("createdAt");
            entity.Property(e => e.UpdatedBy).HasColumnName("updatedBy");
            entity.Property(e => e.UpdatedAt).HasColumnName("updatedAt");
            entity.HasOne(e => e.EmailProvider)
                .WithMany()
                .HasForeignKey(e => e.EmailProviderId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => new { e.UserId, e.Alias })
                .IsUnique()
                .HasFilter("[isDeleted] = 0");
            entity.HasIndex(e => e.UserId)
                .IsUnique()
                .HasFilter("[isDefault] = 1 AND [isDeleted] = 0");
            entity.HasIndex(e => new { e.UserId, e.EmailAddress })
                .IsUnique()
                .HasFilter("[isDeleted] = 0");
            entity.HasIndex(e => new { e.UserId, e.SortOrder })
                .HasFilter("[isDeleted] = 0");
            entity.HasIndex(e => e.EmailProviderId)
                .HasFilter("[isDeleted] = 0");
        });
        modelBuilder.Entity<Contact>(entity =>
        {
            entity.ToTable("Contact", "workspace");
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.UserId).HasColumnName("userId");
            entity.Property(e => e.FirstName).HasColumnName("firstName").HasMaxLength(50);
            entity.Property(e => e.LastName).HasColumnName("lastName").HasMaxLength(50);
            entity.Property(e => e.Email).HasColumnName("email").HasMaxLength(255);
            entity.Property(e => e.Phone).HasColumnName("phone").HasMaxLength(32);
            entity.Property(e => e.Source).HasColumnName("source").HasConversion<byte>();
            entity.Property(e => e.SortOrder).HasColumnName("sortOrder");
            entity.Property(e => e.Alias).HasColumnName("alias").HasMaxLength(64).IsRequired();
            entity.Property(e => e.Context).HasColumnName("context").HasMaxLength(2000);
            entity.Property(e => e.IsActive).HasColumnName("isActive");
            entity.Property(e => e.IsDeleted).HasColumnName("isDeleted");
            entity.Property(e => e.CreatedBy).HasColumnName("createdBy");
            entity.Property(e => e.CreatedAt).HasColumnName("createdAt");
            entity.Property(e => e.UpdatedBy).HasColumnName("updatedBy");
            entity.Property(e => e.UpdatedAt).HasColumnName("updatedAt");
            entity.HasIndex(e => new { e.UserId, e.Email })
                .IsUnique()
                .HasFilter("[isDeleted] = 0 AND [email] IS NOT NULL");
            entity.HasIndex(e => new { e.UserId, e.Alias })
                .IsUnique()
                .HasFilter("[isDeleted] = 0");
            entity.HasIndex(e => new { e.UserId, e.SortOrder })
                .HasFilter("[isDeleted] = 0");
        });
        modelBuilder.Entity<Tag>(entity =>
        {
            entity.ToTable("Tag", "workspace");
            entity.Property(e => e.UserId).HasColumnName("userId");
            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(64);
            entity.Property(e => e.Color).HasColumnName("color").HasMaxLength(9);
            entity.Property(e => e.SortOrder).HasColumnName("sortOrder");
            entity.Property(e => e.IsActive).HasColumnName("isActive");
            entity.Property(e => e.IsDeleted).HasColumnName("isDeleted");
            entity.Property(e => e.CreatedBy).HasColumnName("createdBy");
            entity.Property(e => e.CreatedAt).HasColumnName("createdAt");
            entity.Property(e => e.UpdatedBy).HasColumnName("updatedBy");
            entity.Property(e => e.UpdatedAt).HasColumnName("updatedAt");
            entity.HasIndex(e => new { e.UserId, e.Name })
                .IsUnique()
                .HasFilter("[isDeleted] = 0");
        });
        modelBuilder.Entity<Bucket>(entity =>
        {
            entity.ToTable("Bucket", "workspace");
            entity.Property(e => e.UserId).HasColumnName("userId");
            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(128);
            entity.Property(e => e.SortOrder).HasColumnName("sortOrder");
            entity.Property(e => e.IsActive).HasColumnName("isActive");
            entity.Property(e => e.IsDeleted).HasColumnName("isDeleted");
            entity.Property(e => e.CreatedBy).HasColumnName("createdBy");
            entity.Property(e => e.CreatedAt).HasColumnName("createdAt");
            entity.Property(e => e.UpdatedBy).HasColumnName("updatedBy");
            entity.Property(e => e.UpdatedAt).HasColumnName("updatedAt");
            entity.HasIndex(e => new { e.UserId, e.Name })
                .IsUnique()
                .HasFilter("[isDeleted] = 0");
        });
        modelBuilder.Entity<TagAssignment>(entity =>
        {
            entity.ToTable("TagAssignment", "workspace");
            entity.Property(e => e.UserId).HasColumnName("userId");
            entity.Property(e => e.TagId).HasColumnName("tagId");
            entity.Property(e => e.ReferableKind).HasColumnName("referableKind").HasConversion<byte>();
            entity.Property(e => e.ReferableId).HasColumnName("referableId");
            entity.HasOne(e => e.Tag)
                .WithMany()
                .HasForeignKey(e => e.TagId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => new { e.TagId, e.ReferableKind, e.ReferableId }).IsUnique();
            entity.HasIndex(e => new { e.UserId, e.ReferableKind, e.ReferableId });
        });
        modelBuilder.Entity<BucketMember>(entity =>
        {
            entity.ToTable("BucketMember", "workspace");
            entity.Property(e => e.UserId).HasColumnName("userId");
            entity.Property(e => e.BucketId).HasColumnName("bucketId");
            entity.Property(e => e.ReferableKind).HasColumnName("referableKind").HasConversion<byte>();
            entity.Property(e => e.ReferableId).HasColumnName("referableId");
            entity.HasOne(e => e.Bucket)
                .WithMany()
                .HasForeignKey(e => e.BucketId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => new { e.BucketId, e.ReferableKind, e.ReferableId }).IsUnique();
            entity.HasIndex(e => new { e.UserId, e.ReferableKind, e.ReferableId });
        });
    }
}
