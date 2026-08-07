using Application.Features.EmailProviders;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.EmailAccounts;

public sealed class EmailAccountRepository(IDbContextFactory<AppDbContext> _dbContextFactory)
{
    public async Task<EmailSettings?> GetDefaultEmailSettingsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        await using var ctx = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var row = await ctx.EmailAccounts
            .AsNoTracking()
            .Include(x => x.EmailProvider)
            .Where(x =>
                x.UserId == userId &&
                x.IsDefault &&
                x.IsActive &&
                !x.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);

        if (row?.EmailProvider is null)
        {
            return null;
        }

        return EmailAccountMapping.ToEmailSettings(row, row.EmailProvider);
    }

    public async Task<EmailSettings?> SaveDefaultEmailSettingsAsync(
        Guid userId,
        EmailSettings? settings,
        Guid updatedBy,
        CancellationToken cancellationToken = default)
    {
        await using var ctx = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var now = DateTime.UtcNow;

        var existingDefault = await ctx.EmailAccounts
            .Where(x =>
                x.UserId == userId &&
                x.IsDefault &&
                !x.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);

        if (settings is null)
        {
            if (existingDefault is not null)
            {
                existingDefault.IsDeleted = true;
                existingDefault.IsActive = false;
                existingDefault.IsDefault = false;
                existingDefault.UpdatedBy = updatedBy;
                existingDefault.UpdatedAt = now;
                await ctx.SaveChangesAsync(cancellationToken);
            }

            return null;
        }

        var slug = EmailProviderCatalog.NormalizeSlug(settings.ProviderSlug);
        var provider = await ctx.EmailProviders
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Slug == slug && x.IsActive && !x.IsDeleted, cancellationToken);

        if (provider is null)
        {
            return null;
        }

        EmailAccount entity;
        if (existingDefault is not null)
        {
            entity = existingDefault;
            entity.EmailProviderId = provider.Id;
            entity.EmailAddress = settings.EmailAddress.Trim();
            entity.Username = settings.Username.Trim();
            entity.Password = settings.Password;
            entity.UpdatedBy = updatedBy;
            entity.UpdatedAt = now;
        }
        else
        {
            entity = new EmailAccount
            {
                UserId = userId,
                EmailProviderId = provider.Id,
                Alias = EmailAccountMapping.DefaultAlias,
                EmailAddress = settings.EmailAddress.Trim(),
                Username = settings.Username.Trim(),
                Password = settings.Password,
                IsDefault = true,
                SortOrder = 0,
                CreatedBy = updatedBy,
                UpdatedBy = updatedBy,
                CreatedAt = now,
                UpdatedAt = now
            };
            await ctx.EmailAccounts.AddAsync(entity, cancellationToken);
        }

        await ctx.SaveChangesAsync(cancellationToken);

        return EmailAccountMapping.ToEmailSettings(entity, provider);
    }
}
