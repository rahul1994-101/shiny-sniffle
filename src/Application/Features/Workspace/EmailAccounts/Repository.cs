using Application.Features.Dbo.EmailProviders;
using Application.Features.Shared;
using Application.Utilities.Extensions;
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
            .Where(x => x.UserId == userId)
            .WhereActiveAndNotDeleted()
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

    public async Task<StoredMailboxSettings?> GetStoredMailboxSettingsAsync(
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
                .Where(x => x.UserId == userId && x.IsDefault)
                .WhereActiveAndNotDeleted()
                .FirstOrDefaultAsync(cancellationToken);
        }

        if (row?.EmailProvider is null)
        {
            return null;
        }

        return EmailAccountMapping.ToStoredSettings(row, row.EmailProvider);
    }

    public async Task<StoredMailboxSettings?> GetDefaultStoredMailboxSettingsAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await GetStoredMailboxSettingsAsync(userId, emailAccountId: null, cancellationToken);

    public async Task<EmailAccount?> GetActiveAccountAsync(
        Guid userId,
        string? alias = null,
        CancellationToken cancellationToken = default)
    {
        await using var ctx = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(alias))
        {
            var normalized = EntityAliasRules.SlugifyOptional(alias.Trim()) ?? alias.Trim();
            return await ctx.EmailAccounts
                .AsNoTracking()
                .Include(x => x.EmailProvider)
                .Where(x => x.UserId == userId && x.Alias == normalized)
                .WhereActiveAndNotDeleted()
                .FirstOrDefaultAsync(cancellationToken);
        }

        return await ctx.EmailAccounts
            .AsNoTracking()
            .Include(x => x.EmailProvider)
            .Where(x => x.UserId == userId && x.IsDefault)
            .WhereActiveAndNotDeleted()
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<(EmailAccountDto? Saved, string? Error, bool NotFound)> SaveAsync(
        Guid userId,
        SaveEmailAccountDto dto,
        StoredMailboxSettings builtSettings,
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

        var emailTaken = await ctx.EmailAccounts
            .Where(x => x.UserId == userId && x.EmailAddress == emailAddress && x.Id != dto.Id)
            .WhereNotDeleted()
            .AnyAsync(cancellationToken);

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

        var activeCount = await ctx.EmailAccounts
            .Where(x => x.UserId == userId)
            .WhereNotDeleted()
            .CountAsync(cancellationToken);

        var isCreate = dto.Id is null;
        var makeDefault = isCreate && activeCount == 0;

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
                IsDefault = false
            };
            entity.CreatedBy = updatedBy;
            entity.UpdatedBy = updatedBy;
            entity.CreatedAt = now;
            entity.UpdatedAt = now;
            await ctx.EmailAccounts.AddAsync(entity, cancellationToken);
        }

        await ctx.SaveChangesAsync(cancellationToken);

        if (makeDefault)
        {
            await SetExclusiveDefaultAsync(ctx, userId, entity, updatedBy, now, cancellationToken);
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
        entity.UpdatedBy = updatedBy;
        entity.UpdatedAt = now;
        entity.IsDefault = false;

        if (wasDefault)
        {
            var next = await ctx.EmailAccounts
                .Where(x => x.UserId == userId && x.Id != emailAccountId)
                .WhereActiveAndNotDeleted()
                .OrderBy(x => x.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (next is not null)
            {
                await SetExclusiveDefaultAsync(ctx, userId, next, updatedBy, now, cancellationToken);
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
        await SetExclusiveDefaultAsync(ctx, userId, entity, updatedBy, now, cancellationToken);
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
            .Where(x => x.Id == emailAccountId && x.UserId == userId)
            .WhereActiveAndNotDeleted();

        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// Ensures exactly one default per user. Clears other defaults first, saves, then promotes the target —
    /// avoids filtered unique-index violations when EF batches competing updates.
    /// </summary>
    private static async Task SetExclusiveDefaultAsync(
        AppDbContext ctx,
        Guid userId,
        EmailAccount target,
        Guid updatedBy,
        DateTime now,
        CancellationToken cancellationToken)
    {
        await ClearDefaultExceptAsync(ctx, userId, target.Id, updatedBy, now, cancellationToken);
        await ctx.SaveChangesAsync(cancellationToken);

        if (target.IsDefault)
        {
            return;
        }

        target.IsDefault = true;
        target.UpdatedBy = updatedBy;
        target.UpdatedAt = now;
        await ctx.SaveChangesAsync(cancellationToken);
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
            .Where(x => x.UserId == userId && x.IsDefault && x.Id != keepId)
            .WhereNotDeleted()
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
        ctx.EmailAccounts
            .Where(x => x.UserId == userId && x.Alias == alias && x.Id != excludeId)
            .WhereNotDeleted()
            .AnyAsync(cancellationToken);

    public async Task<(IReadOnlyList<EntityRefMentionItemDto> Items, int TotalCount)> SearchMentionItemsAsync(
        Guid userId,
        string? query,
        IReadOnlyList<string> recentAliases,
        int limit,
        CancellationToken cancellationToken = default)
    {
        await using var ctx = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var baseQuery = ctx.EmailAccounts
            .AsNoTracking()
            .Include(x => x.EmailProvider)
            .Where(x => x.UserId == userId)
            .WhereActiveAndNotDeleted()
            .Where(x => x.EmailProvider != null);

        var totalCount = await baseQuery.CountAsync(cancellationToken);
        if (totalCount == 0)
        {
            return ([], 0);
        }

        var trimmedQuery = query?.Trim();
        List<EmailAccount> rows;

        if (string.IsNullOrEmpty(trimmedQuery))
        {
            rows = await LoadMailboxesForEmptyQueryAsync(baseQuery, recentAliases, limit, cancellationToken);
        }
        else
        {
            rows = await LoadMailboxesForQueryAsync(baseQuery, trimmedQuery, limit, cancellationToken);
        }

        return (rows.ConvertAll(x => ToMentionItem(x, x.EmailProvider!)), totalCount);
    }

    private static async Task<List<EmailAccount>> LoadMailboxesForEmptyQueryAsync(
        IQueryable<EmailAccount> baseQuery,
        IReadOnlyList<string> recentAliases,
        int limit,
        CancellationToken cancellationToken)
    {
        var results = new List<EmailAccount>();
        var usedIds = new HashSet<Guid>();

        if (recentAliases.Count > 0)
        {
            var recentRows = await baseQuery
                .Where(a => recentAliases.Contains(a.Alias))
                .ToListAsync(cancellationToken);

            foreach (var alias in recentAliases)
            {
                var row = recentRows.FirstOrDefault(a =>
                    string.Equals(a.Alias, alias, StringComparison.OrdinalIgnoreCase));

                if (row is not null && usedIds.Add(row.Id))
                {
                    results.Add(row);
                    if (results.Count >= limit)
                    {
                        return results;
                    }
                }
            }
        }

        if (results.Count < limit)
        {
            var filler = await baseQuery
                .Where(a => !usedIds.Contains(a.Id))
                .OrderBy(a => a.Alias)
                .Take(limit - results.Count)
                .ToListAsync(cancellationToken);

            results.AddRange(filler);
        }

        return results;
    }

    private static async Task<List<EmailAccount>> LoadMailboxesForQueryAsync(
        IQueryable<EmailAccount> baseQuery,
        string query,
        int limit,
        CancellationToken cancellationToken)
    {
        var pattern = $"%{query}%";
        var candidates = await baseQuery
            .Where(a =>
                EF.Functions.Like(a.Alias, pattern)
                || EF.Functions.Like(a.EmailAddress, pattern)
                || EF.Functions.Like(a.EmailProvider!.Name, pattern))
            .ToListAsync(cancellationToken);

        return candidates
            .Where(a => EntityRefMentionSearch.MatchesAliasQuery(
                a.Alias,
                a.Alias,
                $"{a.EmailAddress} {a.EmailProvider?.Name}",
                query))
            .OrderBy(a => EntityRefMentionSearch.Rank(
                a.Alias,
                a.Alias,
                a.EmailAddress,
                query))
            .ThenBy(a => a.Alias, StringComparer.OrdinalIgnoreCase)
            .Take(limit)
            .ToList();
    }

    private static EntityRefMentionItemDto ToMentionItem(EmailAccount account, EmailProvider provider)
    {
        var tooltipParts = new List<string> { account.EmailAddress, provider.Name };

        if (account.IsDefault)
        {
            tooltipParts.Add("default");
        }

        return new EntityRefMentionItemDto
        {
            Kind = EntityRefs.Kind.Mailbox,
            Alias = account.Alias,
            PrimaryLabel = account.Alias,
            SecondaryLabel = account.EmailAddress,
            TooltipText = string.Join(" · ", tooltipParts)
        };
    }
}
