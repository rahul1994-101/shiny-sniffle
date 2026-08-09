using Application.Features.EmailProviders;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.EmailAccounts;

public sealed class EmailAccountRepository(IDbContextFactory<AppDbContext> _dbContextFactory)
{
    public async Task<IReadOnlyList<EmailAccountSummaryDto>> ListAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        await using var ctx = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var rows = await ctx.EmailAccounts
            .AsNoTracking()
            .Include(x => x.EmailProvider)
            .Where(x => x.UserId == userId && x.IsActive && !x.IsDeleted)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Alias)
            .ToListAsync(cancellationToken);

        return rows
            .Where(x => x.EmailProvider is not null)
            .Select(x => EmailAccountMapping.ToSummary(x, x.EmailProvider!))
            .ToList();
    }

    public async Task<EmailAccountDto?> GetByIdAsync(Guid userId, Guid accountId, CancellationToken cancellationToken = default)
    {
        await using var ctx = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var row = await FindActiveAccountAsync(ctx, userId, accountId, asNoTracking: true, cancellationToken);
        return row?.EmailProvider is null ? null : EmailAccountMapping.ToDto(row, row.EmailProvider);
    }

    public async Task<EmailSettings?> GetEmailSettingsAsync(
        Guid userId,
        Guid? accountId = null,
        CancellationToken cancellationToken = default)
    {
        await using var ctx = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        EmailAccount? row;

        if (accountId is { } id)
        {
            row = await FindActiveAccountAsync(ctx, userId, id, asNoTracking: true, cancellationToken);
        }
        else
        {
            row = await ctx.EmailAccounts
                .AsNoTracking()
                .Include(x => x.EmailProvider)
                .Where(x =>
                    x.UserId == userId &&
                    x.IsDefault &&
                    x.IsActive &&
                    !x.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);
        }

        if (row?.EmailProvider is null)
        {
            return null;
        }

        return EmailAccountMapping.ToEmailSettings(row, row.EmailProvider);
    }

    public async Task<EmailSettings?> GetDefaultEmailSettingsAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await GetEmailSettingsAsync(userId, accountId: null, cancellationToken);

    public async Task<(EmailAccountDto? Saved, string? Error, bool NotFound)> SaveAsync(
        Guid userId,
        SaveEmailAccountDto dto,
        EmailSettings builtSettings,
        Guid updatedBy,
        CancellationToken cancellationToken = default)
    {
        await using var ctx = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var now = DateTime.UtcNow;
        var emailAddress = builtSettings.EmailAddress.Trim();
        var resolvedAlias = await ResolveAliasAsync(
            ctx, userId, dto.Id, EmailAccountMapping.NormalizeAlias(dto.Alias), emailAddress, cancellationToken);
        var context = string.IsNullOrWhiteSpace(dto.Context) ? null : dto.Context.Trim();

        var aliasTaken = await ctx.EmailAccounts.AnyAsync(
            x =>
                x.UserId == userId &&
                !x.IsDeleted &&
                x.Alias == resolvedAlias &&
                x.Id != dto.Id,
            cancellationToken);

        if (aliasTaken)
        {
            return (null, "An account with this alias already exists.", false);
        }

        var emailTaken = await ctx.EmailAccounts.AnyAsync(
            x =>
                x.UserId == userId &&
                !x.IsDeleted &&
                x.EmailAddress == emailAddress &&
                x.Id != dto.Id,
            cancellationToken);

        if (emailTaken)
        {
            return (null, "This email address is already connected.", false);
        }

        var slug = EmailProviderCatalog.NormalizeSlug(builtSettings.ProviderSlug);
        var provider = await ctx.EmailProviders
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Slug == slug && x.IsActive && !x.IsDeleted, cancellationToken);

        if (provider is null)
        {
            return (null, "Selected provider was not found.", false);
        }

        var activeCount = await ctx.EmailAccounts.CountAsync(
            x => x.UserId == userId && !x.IsDeleted,
            cancellationToken);

        var isCreate = dto.Id is null;
        var makeDefault = dto.IsDefault || (isCreate && activeCount == 0);

        EmailAccount entity;
        if (dto.Id is { } id)
        {
            var existing = await FindActiveAccountAsync(ctx, userId, id, asNoTracking: false, cancellationToken);
            if (existing is null)
            {
                return (null, null, true);
            }

            entity = existing;
            entity.Alias = resolvedAlias;
            entity.EmailProviderId = provider.Id;
            entity.EmailAddress = emailAddress;
            entity.Username = builtSettings.Username.Trim();
            entity.Password = builtSettings.Password;
            entity.Context = context;
            entity.UpdatedBy = updatedBy;
            entity.UpdatedAt = now;

            if (dto.IsDefault)
            {
                makeDefault = true;
            }
        }
        else
        {
            var sortOrder = await ctx.EmailAccounts
                .Where(x => x.UserId == userId && !x.IsDeleted)
                .Select(x => (int?)x.SortOrder)
                .MaxAsync(cancellationToken) ?? 0;

            entity = new EmailAccount
            {
                UserId = userId,
                EmailProviderId = provider.Id,
                Alias = resolvedAlias,
                EmailAddress = emailAddress,
                Username = builtSettings.Username.Trim(),
                Password = builtSettings.Password,
                Context = context,
                IsDefault = false,
                SortOrder = sortOrder + 10,
                CreatedBy = updatedBy,
                UpdatedBy = updatedBy,
                CreatedAt = now,
                UpdatedAt = now
            };
            await ctx.EmailAccounts.AddAsync(entity, cancellationToken);
        }

        if (makeDefault && !isCreate)
        {
            await ClearDefaultExceptAsync(ctx, userId, entity.Id, updatedBy, now, cancellationToken);
            entity.IsDefault = true;
        }

        await ctx.SaveChangesAsync(cancellationToken);

        if (makeDefault && isCreate)
        {
            await ClearDefaultExceptAsync(ctx, userId, entity.Id, updatedBy, now, cancellationToken);
            entity.IsDefault = true;
            entity.UpdatedBy = updatedBy;
            entity.UpdatedAt = now;
            await ctx.SaveChangesAsync(cancellationToken);
        }

        await ctx.Entry(entity).Reference(x => x.EmailProvider).LoadAsync(cancellationToken);
        var providerEntity = entity.EmailProvider ?? provider;
        return (EmailAccountMapping.ToDto(entity, providerEntity), null, false);
    }

    public async Task<(bool Found, string? Error)> SoftDeleteAsync(
        Guid userId,
        Guid accountId,
        Guid updatedBy,
        CancellationToken cancellationToken = default)
    {
        await using var ctx = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var entity = await FindActiveAccountAsync(ctx, userId, accountId, asNoTracking: false, cancellationToken);
        if (entity is null)
        {
            return (false, null);
        }

        var now = DateTime.UtcNow;
        var wasDefault = entity.IsDefault;

        entity.IsDeleted = true;
        entity.IsActive = false;
        entity.IsDefault = false;
        entity.UpdatedBy = updatedBy;
        entity.UpdatedAt = now;

        if (wasDefault)
        {
            var next = await ctx.EmailAccounts
                .Where(x => x.UserId == userId && !x.IsDeleted && x.Id != accountId)
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.Alias)
                .FirstOrDefaultAsync(cancellationToken);

            if (next is not null)
            {
                await ClearDefaultExceptAsync(ctx, userId, next.Id, updatedBy, now, cancellationToken);
                next.IsDefault = true;
                next.UpdatedBy = updatedBy;
                next.UpdatedAt = now;
            }
        }

        await ctx.SaveChangesAsync(cancellationToken);
        return (true, null);
    }

    public async Task<(bool Found, string? Error)> SetDefaultAsync(
        Guid userId,
        Guid accountId,
        Guid updatedBy,
        CancellationToken cancellationToken = default)
    {
        await using var ctx = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var entity = await FindActiveAccountAsync(ctx, userId, accountId, asNoTracking: false, cancellationToken);
        if (entity is null)
        {
            return (false, null);
        }

        var now = DateTime.UtcNow;
        await ClearDefaultExceptAsync(ctx, userId, entity.Id, updatedBy, now, cancellationToken);
        entity.IsDefault = true;
        entity.UpdatedBy = updatedBy;
        entity.UpdatedAt = now;
        await ctx.SaveChangesAsync(cancellationToken);
        return (true, null);
    }

    public async Task<bool> IsAliasTakenAsync(
        Guid userId,
        string alias,
        Guid? excludeId,
        CancellationToken cancellationToken = default)
    {
        var normalized = alias.Trim();
        await using var ctx = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await ctx.EmailAccounts.AnyAsync(
            x => x.UserId == userId && !x.IsDeleted && x.Alias == normalized && x.Id != excludeId,
            cancellationToken);
    }

    private static async Task<EmailAccount?> FindActiveAccountAsync(
        AppDbContext ctx,
        Guid userId,
        Guid accountId,
        bool asNoTracking,
        CancellationToken cancellationToken)
    {
        var query = ctx.EmailAccounts
            .Include(x => x.EmailProvider)
            .Where(x =>
                x.Id == accountId &&
                x.UserId == userId &&
                x.IsActive &&
                !x.IsDeleted);

        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    private static async Task ClearDefaultExceptAsync(
        AppDbContext ctx,
        Guid userId,
        Guid keepId,
        Guid updatedBy,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var others = await ctx.EmailAccounts
            .Where(x =>
                x.UserId == userId &&
                !x.IsDeleted &&
                x.IsDefault &&
                x.Id != keepId)
            .ToListAsync(cancellationToken);

        foreach (var row in others)
        {
            row.IsDefault = false;
            row.UpdatedBy = updatedBy;
            row.UpdatedAt = now;
        }
    }

    private static async Task<string> ResolveAliasAsync(
        AppDbContext ctx,
        Guid userId,
        Guid? excludeAccountId,
        string? normalizedAlias,
        string emailAddress,
        CancellationToken cancellationToken)
    {
        if (normalizedAlias is not null)
        {
            return normalizedAlias;
        }

        var stem = EmailAccountMapping.BuildAliasStem(emailAddress);
        for (var index = 1; index < 10_000; index++)
        {
            var candidate = EmailAccountMapping.AliasWithNumericSuffix(stem, index);
            var taken = await ctx.EmailAccounts.AnyAsync(
                x =>
                    x.UserId == userId &&
                    !x.IsDeleted &&
                    x.Alias == candidate &&
                    x.Id != excludeAccountId,
                cancellationToken);

            if (!taken)
            {
                return candidate;
            }
        }

        return EmailAccountMapping.AliasWithNumericSuffix(stem, Random.Shared.Next(1000, 9999));
    }
}
