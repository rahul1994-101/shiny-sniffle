using Application.Features.Shared;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Workspace.Buckets;

public sealed class BucketRepository(
    IDbContextFactory<AppDbContext> dbContextFactory,
    SharedRepository sharedRepo)
{
    public async Task<IReadOnlyList<BucketDto>> GetAllBucketsByUserIdAsync(
        Guid userId,
        bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        await using var ctx = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var query = ctx.Buckets
            .AsNoTracking()
            .Where(x => x.UserId == userId);

        query = includeInactive ? query.WhereNotDeleted() : query.WhereActiveAndNotDeleted();

        var rows = await query
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        return rows.ConvertAll(BucketDto.FromEntity);
    }

    public async Task<BucketDto?> GetBucketByIdAsync(Guid userId, Guid bucketId, CancellationToken cancellationToken = default)
    {
        await using var ctx = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var row = await FindOwnedAsync(ctx, userId, bucketId, activeOnly: false, asNoTracking: true, cancellationToken);
        return row is null ? null : BucketDto.FromEntity(row);
    }

    public async Task<(BucketDto? Saved, string? Error, bool NotFound)> SaveAsync(
        Guid userId,
        SaveBucketDto dto,
        Guid updatedBy,
        CancellationToken cancellationToken = default)
    {
        await using var ctx = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var name = BucketMapping.NormalizeName(dto.Name);
        var color = CatalogFieldRules.NormalizeColor(dto.Color);
        var context = CatalogFieldRules.NormalizeContext(dto.Context);
        var now = DateTime.UtcNow;

        Bucket? existing = null;
        if (dto.Id is { } existingId)
        {
            existing = await FindOwnedAsync(ctx, userId, existingId, activeOnly: false, asNoTracking: false, cancellationToken);
            if (existing is null)
            {
                return (null, null, true);
            }
        }

        var alias = await WorkspaceErAliasResolver.ResolveAsync(
            (candidate, excludeId, ct) => IsAliasTakenAsync(ctx, userId, candidate, excludeId, ct),
            EntityRefs.Kind.Bucket,
            dto.Alias,
            dto.Id,
            existing?.Alias,
            name,
            secondarySource: null,
            cancellationToken);

        if (await IsAliasTakenAsync(ctx, userId, alias, dto.Id, cancellationToken))
        {
            return (null, "A bucket with this alias already exists.", false);
        }

        Bucket entity;
        if (existing is not null)
        {
            entity = existing;
            entity.Name = name;
            entity.Alias = alias;
            entity.Color = color;
            entity.Context = context;
            entity.UpdatedBy = updatedBy;
            entity.UpdatedAt = now;
        }
        else
        {
            entity = new Bucket
            {
                UserId = userId,
                Name = name,
                Alias = alias,
                Color = color,
                Context = context
            };
            entity.CreatedBy = updatedBy;
            entity.UpdatedBy = updatedBy;
            entity.CreatedAt = now;
            entity.UpdatedAt = now;
            await ctx.Buckets.AddAsync(entity, cancellationToken);
        }

        try
        {
            await ctx.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            return (null, BucketMapping.MapSaveError(ex), false);
        }

        return (BucketDto.FromEntity(entity), null, false);
    }

    public async Task<bool> SetActiveAsync(
        Guid userId,
        Guid bucketId,
        bool isActive,
        Guid updatedBy,
        CancellationToken cancellationToken = default)
    {
        await using var ctx = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var entity = await FindOwnedAsync(ctx, userId, bucketId, activeOnly: false, asNoTracking: false, cancellationToken);
        if (entity is null || entity.IsActive == isActive)
        {
            return entity is not null;
        }

        entity.IsActive = isActive;
        entity.UpdatedBy = updatedBy;
        entity.UpdatedAt = DateTime.UtcNow;
        await ctx.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(
        Guid userId,
        Guid bucketId,
        Guid updatedBy,
        CancellationToken cancellationToken = default)
    {
        await using var ctx = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var entity = await FindOwnedAsync(ctx, userId, bucketId, activeOnly: false, asNoTracking: false, cancellationToken);
        if (entity is null)
        {
            return false;
        }

        entity.IsDeleted = true;
        entity.IsActive = false;
        entity.UpdatedBy = updatedBy;
        entity.UpdatedAt = DateTime.UtcNow;

        await sharedRepo.RemoveBucketAssignmentsAsync(ctx, userId, bucketId, cancellationToken);
        await ctx.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static Task<bool> IsAliasTakenAsync(
        AppDbContext ctx,
        Guid userId,
        string alias,
        Guid? excludeId,
        CancellationToken cancellationToken) =>
        ctx.Buckets
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
        await using var ctx = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var baseQuery = ctx.Buckets
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .WhereActiveAndNotDeleted();

        var totalCount = await baseQuery.CountAsync(cancellationToken);
        if (totalCount == 0)
        {
            return ([], 0);
        }

        var trimmedQuery = query?.Trim();
        List<Bucket> rows;

        if (string.IsNullOrEmpty(trimmedQuery))
        {
            rows = await LoadBucketsForEmptyQueryAsync(baseQuery, recentAliases, limit, cancellationToken);
        }
        else
        {
            rows = await LoadBucketsForQueryAsync(baseQuery, trimmedQuery, limit, cancellationToken);
        }

        return (rows.ConvertAll(ToMentionItem), totalCount);
    }

    private static async Task<List<Bucket>> LoadBucketsForEmptyQueryAsync(
        IQueryable<Bucket> baseQuery,
        IReadOnlyList<string> recentAliases,
        int limit,
        CancellationToken cancellationToken)
    {
        var results = new List<Bucket>();
        var usedIds = new HashSet<Guid>();

        if (recentAliases.Count > 0)
        {
            var recentRows = await baseQuery
                .Where(b => recentAliases.Contains(b.Alias))
                .ToListAsync(cancellationToken);

            foreach (var alias in recentAliases)
            {
                var row = recentRows.FirstOrDefault(b =>
                    string.Equals(b.Alias, alias, StringComparison.OrdinalIgnoreCase));

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
                .Where(b => !usedIds.Contains(b.Id))
                .OrderBy(b => b.Name)
                .Take(limit - results.Count)
                .ToListAsync(cancellationToken);

            results.AddRange(filler);
        }

        return results;
    }

    private static async Task<List<Bucket>> LoadBucketsForQueryAsync(
        IQueryable<Bucket> baseQuery,
        string query,
        int limit,
        CancellationToken cancellationToken)
    {
        var pattern = $"%{query}%";
        var candidates = await baseQuery
            .Where(b =>
                EF.Functions.Like(b.Alias, pattern)
                || EF.Functions.Like(b.Name, pattern)
                || (b.Context != null && EF.Functions.Like(b.Context, pattern)))
            .ToListAsync(cancellationToken);

        return candidates
            .Where(b => EntityRefMentionSearch.MatchesAliasQuery(b.Alias, b.Name, b.Context, query))
            .OrderBy(b => EntityRefMentionSearch.Rank(b.Alias, b.Name, b.Context, query))
            .ThenBy(b => b.Name, StringComparer.OrdinalIgnoreCase)
            .Take(limit)
            .ToList();
    }

    private static EntityRefMentionItemDto ToMentionItem(Bucket entity) => new()
    {
        Kind = EntityRefs.Kind.Bucket,
        Alias = entity.Alias,
        PrimaryLabel = entity.Name,
        SecondaryLabel = $"@{entity.Alias}",
        TooltipText = BuildCatalogTooltip(entity.Name, entity.Alias, entity.Context)
    };

    private static string BuildCatalogTooltip(string name, string alias, string? context)
    {
        var parts = new List<string> { name, $"@{alias}" };

        if (!string.IsNullOrWhiteSpace(context))
        {
            parts.Add(context.Trim());
        }

        return string.Join(" · ", parts);
    }

    private static async Task<Bucket?> FindOwnedAsync(
        AppDbContext ctx,
        Guid userId,
        Guid bucketId,
        bool activeOnly,
        bool asNoTracking,
        CancellationToken cancellationToken)
    {
        var query = ctx.Buckets
            .Where(x => x.Id == bucketId && x.UserId == userId)
            .WhereNotDeleted();

        if (activeOnly)
        {
            query = query.WhereIsActive();
        }

        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync(cancellationToken);
    }
}
