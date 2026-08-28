using Application.Features.Shared;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Workspace.Contacts;

public sealed class ContactRepository(
    IDbContextFactory<AppDbContext> _dbContextFactory,
    SharedRepository _sharedRepo)
{
    public async Task<IReadOnlyList<ContactSummaryDto>> GetAllContactsByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        await using var ctx = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var rows = await ctx.Contacts
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .WhereActiveAndNotDeleted()
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        var ids = rows.ConvertAll(x => x.Id);
        var taxonomy = await _sharedRepo.LoadTaxonomyForReferablesAsync(
            ctx,
            userId,
            ReferableKind.Contact,
            ids,
            cancellationToken);

        return rows.ConvertAll(entity =>
        {
            taxonomy.TryGetValue(entity.Id, out var tax);
            return ContactSummaryDto.FromEntity(entity, tax);
        });
    }

    public async Task<ContactDto?> GetContactByIdAsync(Guid userId, Guid contactId, CancellationToken cancellationToken = default)
    {
        await using var ctx = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var row = await FindActiveAsync(ctx, userId, contactId, asNoTracking: true, cancellationToken);
        if (row is null)
        {
            return null;
        }

        var taxonomy = await _sharedRepo.LoadTaxonomyForReferablesAsync(
            ctx,
            userId,
            ReferableKind.Contact,
            [row.Id],
            cancellationToken);
        taxonomy.TryGetValue(row.Id, out var tax);
        return ContactDto.FromEntity(row, tax);
    }

    public async Task<(ContactDto? Saved, string? Error, bool NotFound)> SaveAsync(
        Guid userId,
        SaveContactDto dto,
        Guid updatedBy,
        CancellationToken cancellationToken = default)
    {
        await using var ctx = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var now = DateTime.UtcNow;
        var firstName = dto.FirstName.Trim();
        var lastName = dto.LastName.Trim();
        var email = ContactMapping.NormalizeEmail(dto.Email);
        var phone = ContactMapping.NormalizePhone(dto.Phone);
        var context = CatalogFieldRules.NormalizeContext(dto.Context);

        Contact? existing = null;
        if (dto.Id is { } existingId)
        {
            existing = await FindActiveAsync(ctx, userId, existingId, asNoTracking: false, cancellationToken);
            if (existing is null)
            {
                return (null, null, true);
            }
        }

        var resolvedAlias = await WorkspaceErAliasResolver.ResolveAsync(
            (candidate, excludeId, ct) => IsAliasTakenAsync(ctx, userId, candidate, excludeId, ct),
            EntityRefs.Kind.Contact,
            dto.Alias,
            dto.Id,
            existing?.Alias,
            firstName,
            lastName,
            cancellationToken);

        if (email is not null)
        {
            var emailTaken = await ctx.Contacts
                .Where(x => x.UserId == userId && x.Email == email && x.Id != dto.Id)
                .WhereNotDeleted()
                .AnyAsync(cancellationToken);

            if (emailTaken)
            {
                return (null, "A contact with this email already exists.", false);
            }
        }

        if (await IsAliasTakenAsync(ctx, userId, resolvedAlias, dto.Id, cancellationToken))
        {
            return (null, "A contact with this alias already exists.", false);
        }

        Contact entity;

        if (existing is not null)
        {
            entity = existing;
            entity.FirstName = firstName;
            entity.LastName = lastName;
            entity.Alias = resolvedAlias;
            entity.Email = email;
            entity.Phone = phone;
            entity.Context = context;
            entity.UpdatedBy = updatedBy;
            entity.UpdatedAt = now;
        }
        else
        {
            entity = new Contact
            {
                UserId = userId,
                FirstName = firstName,
                LastName = lastName,
                Alias = resolvedAlias,
                Email = email,
                Phone = phone,
                Context = context,
                Source = ContactSource.Manual
            };
            entity.CreatedBy = updatedBy;
            entity.UpdatedBy = updatedBy;
            entity.CreatedAt = now;
            entity.UpdatedAt = now;
            await ctx.Contacts.AddAsync(entity, cancellationToken);
        }

        try
        {
            await ctx.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            return (null, ContactMapping.MapSaveError(ex), false);
        }

        var (syncOk, syncError) = await _sharedRepo.SyncTaxonomyAsync(
            ctx,
            userId,
            ReferableKind.Contact,
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
            ReferableKind.Contact,
            [entity.Id],
            cancellationToken);
        taxonomy.TryGetValue(entity.Id, out var tax);
        return (ContactDto.FromEntity(entity, tax), null, false);
    }

    public async Task<bool> DeleteAsync(
        Guid userId,
        Guid contactId,
        Guid updatedBy,
        CancellationToken cancellationToken = default)
    {
        await using var ctx = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var entity = await FindActiveAsync(ctx, userId, contactId, asNoTracking: false, cancellationToken);
        if (entity is null)
        {
            return false;
        }

        entity.IsDeleted = true;
        entity.IsActive = false;
        entity.UpdatedBy = updatedBy;
        entity.UpdatedAt = DateTime.UtcNow;

        await _sharedRepo.RemoveTaxonomyForReferableAsync(ctx, userId, ReferableKind.Contact, contactId, cancellationToken);
        await ctx.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static async Task<Contact?> FindActiveAsync(
        AppDbContext ctx,
        Guid userId,
        Guid contactId,
        bool asNoTracking,
        CancellationToken cancellationToken)
    {
        var query = ctx.Contacts
            .Where(x => x.Id == contactId && x.UserId == userId)
            .WhereActiveAndNotDeleted();

        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    private static Task<bool> IsAliasTakenAsync(
        AppDbContext ctx,
        Guid userId,
        string alias,
        Guid? excludeId,
        CancellationToken cancellationToken) =>
        ctx.Contacts
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
        var baseQuery = ctx.Contacts
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .WhereActiveAndNotDeleted();

        var totalCount = await baseQuery.CountAsync(cancellationToken);
        if (totalCount == 0)
        {
            return ([], 0);
        }

        var trimmedQuery = query?.Trim();
        List<Contact> rows;

        if (string.IsNullOrEmpty(trimmedQuery))
        {
            rows = await LoadContactsForEmptyQueryAsync(baseQuery, recentAliases, limit, cancellationToken);
        }
        else
        {
            rows = await LoadContactsForQueryAsync(baseQuery, trimmedQuery, limit, cancellationToken);
        }

        return (rows.ConvertAll(ToMentionItem), totalCount);
    }

    private static async Task<List<Contact>> LoadContactsForEmptyQueryAsync(
        IQueryable<Contact> baseQuery,
        IReadOnlyList<string> recentAliases,
        int limit,
        CancellationToken cancellationToken)
    {
        var results = new List<Contact>();
        var usedIds = new HashSet<Guid>();

        if (recentAliases.Count > 0)
        {
            var recentRows = await baseQuery
                .Where(c => recentAliases.Contains(c.Alias))
                .ToListAsync(cancellationToken);

            foreach (var alias in recentAliases)
            {
                var row = recentRows.FirstOrDefault(c =>
                    string.Equals(c.Alias, alias, StringComparison.OrdinalIgnoreCase));

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
                .Where(c => !usedIds.Contains(c.Id))
                .OrderBy(c => c.FirstName)
                .ThenBy(c => c.LastName)
                .Take(limit - results.Count)
                .ToListAsync(cancellationToken);

            results.AddRange(filler);
        }

        return results;
    }

    private static async Task<List<Contact>> LoadContactsForQueryAsync(
        IQueryable<Contact> baseQuery,
        string query,
        int limit,
        CancellationToken cancellationToken)
    {
        var pattern = $"%{query}%";
        var candidates = await baseQuery
            .Where(c =>
                EF.Functions.Like(c.Alias, pattern)
                || EF.Functions.Like(c.FirstName, pattern)
                || EF.Functions.Like(c.LastName, pattern)
                || (c.Email != null && EF.Functions.Like(c.Email, pattern)))
            .ToListAsync(cancellationToken);

        return candidates
            .Where(c => EntityRefMentionSearch.MatchesAliasQuery(
                c.Alias,
                ContactMapping.ResolveListLabel(c),
                c.Email,
                query))
            .OrderBy(c => EntityRefMentionSearch.Rank(
                c.Alias,
                ContactMapping.ResolveListLabel(c),
                c.Email,
                query))
            .ThenBy(c => ContactMapping.ResolveListLabel(c), StringComparer.OrdinalIgnoreCase)
            .Take(limit)
            .ToList();
    }

    private static EntityRefMentionItemDto ToMentionItem(Contact entity)
    {
        var label = ContactMapping.ResolveListLabel(entity);
        var tooltipParts = new List<string> { label };

        if (!string.IsNullOrWhiteSpace(entity.Email))
        {
            tooltipParts.Add(entity.Email);
        }

        if (!string.IsNullOrWhiteSpace(entity.Phone))
        {
            tooltipParts.Add(entity.Phone);
        }

        return new EntityRefMentionItemDto
        {
            Kind = EntityRefs.Kind.Contact,
            Alias = entity.Alias,
            PrimaryLabel = label,
            SecondaryLabel = $"@{entity.Alias}",
            AvatarText = ComputeInitials(label),
            TooltipText = string.Join(" · ", tooltipParts)
        };
    }

    private static string ComputeInitials(string name)
    {
        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0)
        {
            return "?";
        }

        if (parts.Length == 1)
        {
            return parts[0][..Math.Min(2, parts[0].Length)].ToUpperInvariant();
        }

        return $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant();
    }
}
