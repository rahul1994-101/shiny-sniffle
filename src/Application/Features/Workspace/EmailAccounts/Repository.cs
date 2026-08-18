using Application.Features.Dbo.EmailProviders;
using Application.Utilities.Extensions;
using Infrastructure.Mailbox;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Workspace.EmailAccounts;

public sealed class EmailAccountRepository(
    IDbContextFactory<AppDbContext> _dbContextFactory,
    SharedRepository _sharedRepo,
    EmailProviderRepository _emailProviderRepo)
{
    public async Task<IReadOnlyList<EmailAccountSummaryDto>> GetAllEmailAccountsByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        await using var ctx = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var rows = await ctx.EmailAccounts
            .AsNoTracking()
            .Include(x => x.EmailProvider)
            .Where(x => x.UserId == userId && x.IsActive && !x.IsDeleted)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        var accounts = rows.Where(x => x.EmailProvider is not null).ToList();
        var ids = accounts.ConvertAll(x => x.Id);
        var taxonomy = await _sharedRepo.LoadTaxonomyForReferablesAsync(
            ctx,
            userId,
            ReferableKind.Mailbox,
            ids,
            cancellationToken);

        return accounts
            .Select(x =>
            {
                taxonomy.TryGetValue(x.Id, out var tax);
                return EmailAccountSummaryDto.FromEntity(x, x.EmailProvider!, tax);
            })
            .ToList();
    }

    public async Task<EmailAccountDto?> GetEmailAccountByIdAsync(Guid userId, Guid emailAccountId, CancellationToken cancellationToken = default)
    {
        await using var ctx = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var row = await FindActiveAccountAsync(ctx, userId, emailAccountId, asNoTracking: true, cancellationToken);
        if (row?.EmailProvider is null)
        {
            return null;
        }

        var taxonomy = await _sharedRepo.LoadTaxonomyForReferablesAsync(
            ctx,
            userId,
            ReferableKind.Mailbox,
            [row.Id],
            cancellationToken);
        taxonomy.TryGetValue(row.Id, out var tax);
        return EmailAccountDto.FromEntity(row, row.EmailProvider, tax);
    }

    public async Task<EmailSettings?> GetEmailSettingsAsync(
        Guid userId,
        Guid? emailAccountId = null,
        CancellationToken cancellationToken = default)
    {
        await using var ctx = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        EmailAccount? row;

        if (emailAccountId is { } id)
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
        await GetEmailSettingsAsync(userId, emailAccountId: null, cancellationToken);

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
        var context = CatalogFieldRules.NormalizeContext(dto.Context);

        EmailAccount? existing = null;
        if (dto.Id is { } existingId)
        {
            existing = await FindActiveAccountAsync(ctx, userId, existingId, asNoTracking: false, cancellationToken);
            if (existing is null)
            {
                return (null, null, true);
            }
        }

        var resolvedAlias = await WorkspaceErAliasResolver.ResolveAsync(
            (candidate, excludeId, ct) => IsAliasTakenAsync(ctx, userId, candidate, excludeId, ct),
            EntityRefs.Kind.Mailbox,
            dto.Alias,
            dto.Id,
            existing?.Alias,
            emailAddress,
            secondarySource: null,
            cancellationToken);

        if (await IsAliasTakenAsync(ctx, userId, resolvedAlias, dto.Id, cancellationToken))
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

        var provider = await _emailProviderRepo.GetEmailProviderByIdAsync(
            userId,
            dto.EmailProviderId,
            cancellationToken);

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
        if (existing is not null)
        {
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
        var providerEntity = entity.EmailProvider ?? provider.ToEntity();

        var (syncOk, syncError) = await _sharedRepo.SyncTaxonomyAsync(
            ctx,
            userId,
            ReferableKind.Mailbox,
            entity.Id,
            dto.TagIds.ToList(),
            dto.BucketIds.ToList(),
            cancellationToken);

        if (!syncOk)
        {
            return (null, syncError, false);
        }

        await ctx.SaveChangesAsync(cancellationToken);

        var taxonomy = await _sharedRepo.LoadTaxonomyForReferablesAsync(
            ctx,
            userId,
            ReferableKind.Mailbox,
            [entity.Id],
            cancellationToken);
        taxonomy.TryGetValue(entity.Id, out var tax);
        return (EmailAccountDto.FromEntity(entity, providerEntity, tax), null, false);
    }

    public async Task<(bool Found, string? Error)> DeleteAsync(
        Guid userId,
        Guid emailAccountId,
        Guid updatedBy,
        CancellationToken cancellationToken = default)
    {
        await using var ctx = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var entity = await FindActiveAccountAsync(ctx, userId, emailAccountId, asNoTracking: false, cancellationToken);
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
                .Where(x => x.UserId == userId && !x.IsDeleted && x.Id != emailAccountId)
                .OrderBy(x => x.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (next is not null)
            {
                await ClearDefaultExceptAsync(ctx, userId, next.Id, updatedBy, now, cancellationToken);
                next.IsDefault = true;
                next.UpdatedBy = updatedBy;
                next.UpdatedAt = now;
            }
        }

        await _sharedRepo.RemoveTaxonomyForReferableAsync(ctx, userId, ReferableKind.Mailbox, emailAccountId, cancellationToken);
        await ctx.SaveChangesAsync(cancellationToken);
        return (true, null);
    }

    public async Task<(bool Found, string? Error)> SetDefaultAsync(
        Guid userId,
        Guid emailAccountId,
        Guid updatedBy,
        CancellationToken cancellationToken = default)
    {
        await using var ctx = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var entity = await FindActiveAccountAsync(ctx, userId, emailAccountId, asNoTracking: false, cancellationToken);
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

    private static async Task<EmailAccount?> FindActiveAccountAsync(
        AppDbContext ctx,
        Guid userId,
        Guid emailAccountId,
        bool asNoTracking,
        CancellationToken cancellationToken)
    {
        var query = ctx.EmailAccounts
            .Include(x => x.EmailProvider)
            .Where(x =>
                x.Id == emailAccountId &&
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

    private static Task<bool> IsAliasTakenAsync(
        AppDbContext ctx,
        Guid userId,
        string alias,
        Guid? excludeId,
        CancellationToken cancellationToken) =>
        ctx.EmailAccounts.AnyAsync(
            x =>
                x.UserId == userId &&
                !x.IsDeleted &&
                x.Alias == alias &&
                x.Id != excludeId,
            cancellationToken);
}
