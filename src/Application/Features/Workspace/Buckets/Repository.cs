using Application.Features.Shared;
using Infrastructure.Persistence;
using Infrastructure.Persistence.workspace;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.workspace.Buckets;

public sealed class BucketRepository(
    IDbContextFactory<AppDbContext> dbContextFactory,
    ErTaxonomyRepository taxonomyRepo)
{
    public async Task<IReadOnlyList<BucketDto>> ListAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        await using var ctx = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var rows = await ctx.Buckets
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.IsActive && !x.IsDeleted)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);

        return rows.ConvertAll(BucketMapping.ToDto);
    }

    public async Task<(BucketDto? Saved, string? Error, bool NotFound)> SaveAsync(
        Guid userId,
        SaveBucketDto dto,
        Guid updatedBy,
        CancellationToken cancellationToken = default)
    {
        await using var ctx = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var name = BucketMapping.NormalizeName(dto.Name);
        var now = DateTime.UtcNow;

        if (await IsNameTakenAsync(ctx, userId, name, dto.Id, cancellationToken))
        {
            return (null, "A bucket with this name already exists.", false);
        }

        Bucket entity;
        if (dto.Id is { } id)
        {
            var existing = await FindActiveAsync(ctx, userId, id, track: true, cancellationToken);
            if (existing is null)
            {
                return (null, null, true);
            }

            entity = existing;
            entity.Name = name;
            entity.UpdatedBy = updatedBy;
            entity.UpdatedAt = now;
        }
        else
        {
            var sortOrder = await ctx.Buckets
                .Where(x => x.UserId == userId && !x.IsDeleted)
                .Select(x => (int?)x.SortOrder)
                .MaxAsync(cancellationToken) ?? 0;

            entity = new Bucket
            {
                UserId = userId,
                Name = name,
                SortOrder = sortOrder + 10,
                CreatedBy = updatedBy,
                UpdatedBy = updatedBy,
                CreatedAt = now,
                UpdatedAt = now
            };
            await ctx.Buckets.AddAsync(entity, cancellationToken);
        }

        await ctx.SaveChangesAsync(cancellationToken);
        return (BucketMapping.ToDto(entity), null, false);
    }

    public async Task<bool> SoftDeleteAsync(
        Guid userId,
        Guid bucketId,
        Guid updatedBy,
        CancellationToken cancellationToken = default)
    {
        await using var ctx = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var entity = await FindActiveAsync(ctx, userId, bucketId, track: true, cancellationToken);
        if (entity is null)
        {
            return false;
        }

        entity.IsDeleted = true;
        entity.IsActive = false;
        entity.UpdatedBy = updatedBy;
        entity.UpdatedAt = DateTime.UtcNow;

        await taxonomyRepo.RemoveMembersForBucketAsync(ctx, userId, bucketId, cancellationToken);
        await ctx.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static async Task<bool> IsNameTakenAsync(
        AppDbContext ctx,
        Guid userId,
        string name,
        Guid? excludeId,
        CancellationToken cancellationToken)
    {
        var lower = name.ToLowerInvariant();
        return await ctx.Buckets.AnyAsync(
            x =>
                x.UserId == userId &&
                !x.IsDeleted &&
                x.Name.ToLower() == lower &&
                x.Id != excludeId,
            cancellationToken);
    }

    private static async Task<Bucket?> FindActiveAsync(
        AppDbContext ctx,
        Guid userId,
        Guid bucketId,
        bool track,
        CancellationToken cancellationToken)
    {
        var query = ctx.Buckets.Where(x =>
            x.Id == bucketId &&
            x.UserId == userId &&
            x.IsActive &&
            !x.IsDeleted);

        if (!track)
        {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync(cancellationToken);
    }
}
