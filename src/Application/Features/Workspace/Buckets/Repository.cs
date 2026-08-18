using Application.Features.Shared;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Workspace.Buckets;

public sealed class BucketRepository(
    IDbContextFactory<AppDbContext> dbContextFactory,
    SharedRepository sharedRepo)
{
    public async Task<IReadOnlyList<BucketDto>> GetAllBucketsByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        await using var ctx = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var rows = await ctx.Buckets
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .WhereActive()
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        return rows.ConvertAll(BucketDto.FromEntity);
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
            existing = await FindActiveAsync(ctx, userId, existingId, asNoTracking: false, cancellationToken);
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

        await ctx.SaveChangesAsync(cancellationToken);
        return (BucketDto.FromEntity(entity), null, false);
    }

    public async Task<bool> DeleteAsync(
        Guid userId,
        Guid bucketId,
        Guid updatedBy,
        CancellationToken cancellationToken = default)
    {
        await using var ctx = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var entity = await FindActiveAsync(ctx, userId, bucketId, asNoTracking: false, cancellationToken);
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
        ctx.Buckets.AnyAsync(
            x =>
                x.UserId == userId &&
                !x.IsDeleted &&
                x.Alias == alias &&
                x.Id != excludeId,
            cancellationToken);

    private static async Task<Bucket?> FindActiveAsync(
        AppDbContext ctx,
        Guid userId,
        Guid bucketId,
        bool asNoTracking,
        CancellationToken cancellationToken)
    {
        var query = ctx.Buckets
            .Where(x => x.Id == bucketId && x.UserId == userId)
            .WhereActive();

        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync(cancellationToken);
    }
}
