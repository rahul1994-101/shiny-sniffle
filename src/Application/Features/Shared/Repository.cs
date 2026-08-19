using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Shared;

/// <summary>
/// Cross-slice data access only — queries spanning multiple feature folders.
/// AI/Services use slice repos for single-slice data; inject Shared when slices combine.
/// </summary>
public sealed class SharedRepository
{
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

    public SharedRepository(IDbContextFactory<AppDbContext> dbContextFactory) =>
        _dbContextFactory = dbContextFactory;

    public async Task<IReadOnlyDictionary<Guid, ErTaxonomyDto>> LoadTaxonomyForReferablesAsync(
        AppDbContext ctx,
        Guid userId,
        ReferableKind kind,
        IReadOnlyCollection<Guid> referableIds,
        CancellationToken cancellationToken = default)
    {
        if (referableIds.Count == 0)
        {
            return new Dictionary<Guid, ErTaxonomyDto>();
        }

        var tagRows = await ctx.TagAssignments
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.ReferableKind == kind && referableIds.Contains(x.ReferableId))
            .Join(
                ctx.Tags.AsNoTracking().Where(t => t.UserId == userId).WhereActiveAndNotDeleted(),
                a => a.TagId,
                t => t.Id,
                (a, t) => new { a.ReferableId, Tag = t })
            .ToListAsync(cancellationToken);

        var bucketRows = await ctx.BucketAssignments
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.ReferableKind == kind && referableIds.Contains(x.ReferableId))
            .Join(
                ctx.Buckets.AsNoTracking().Where(b => b.UserId == userId).WhereActiveAndNotDeleted(),
                m => m.BucketId,
                b => b.Id,
                (m, b) => new { m.ReferableId, Bucket = b })
            .ToListAsync(cancellationToken);

        var map = referableIds.ToDictionary(id => id, _ => EmptyTaxonomy());

        foreach (var group in tagRows.GroupBy(x => x.ReferableId))
        {
            var current = map[group.Key];
            map[group.Key] = new ErTaxonomyDto
            {
                Tags = group
                    .Select(x => new TagRefDto
                    {
                        Id = x.Tag.Id,
                        Name = x.Tag.Name,
                        Alias = x.Tag.Alias,
                        Color = x.Tag.Color,
                        Context = x.Tag.Context
                    })
                    .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                    .ToList(),
                Buckets = current.Buckets
            };
        }

        foreach (var group in bucketRows.GroupBy(x => x.ReferableId))
        {
            var current = map[group.Key];
            map[group.Key] = new ErTaxonomyDto
            {
                Tags = current.Tags,
                Buckets = group
                    .Select(x => new BucketRefDto
                    {
                        Id = x.Bucket.Id,
                        Name = x.Bucket.Name,
                        Alias = x.Bucket.Alias,
                        Color = x.Bucket.Color,
                        Context = x.Bucket.Context
                    })
                    .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                    .ToList()
            };
        }

        return map;
    }

    public async Task<(bool Ok, string? Error)> SyncTaxonomyAsync(
        AppDbContext ctx,
        Guid userId,
        ReferableKind kind,
        Guid referableId,
        IReadOnlyList<Guid> tagIds,
        IReadOnlyList<Guid> bucketIds,
        CancellationToken cancellationToken = default)
    {
        var distinctTags = tagIds.Distinct().ToList();
        var distinctBuckets = bucketIds.Distinct().ToList();

        if (distinctTags.Count > 0)
        {
            var validCount = await ctx.Tags
                .Where(t => t.UserId == userId)
                .WhereActiveAndNotDeleted()
                .CountAsync(t => distinctTags.Contains(t.Id), cancellationToken);

            if (validCount != distinctTags.Count)
            {
                return (false, "One or more tags are invalid.");
            }
        }

        if (distinctBuckets.Count > 0)
        {
            var validCount = await ctx.Buckets
                .Where(b => b.UserId == userId)
                .WhereActiveAndNotDeleted()
                .CountAsync(b => distinctBuckets.Contains(b.Id), cancellationToken);

            if (validCount != distinctBuckets.Count)
            {
                return (false, "One or more buckets are invalid.");
            }
        }

        var existingTags = await ctx.TagAssignments
            .Where(x => x.UserId == userId && x.ReferableKind == kind && x.ReferableId == referableId)
            .ToListAsync(cancellationToken);
        ctx.TagAssignments.RemoveRange(existingTags);

        foreach (var tagId in distinctTags)
        {
            await ctx.TagAssignments.AddAsync(
                new TagAssignment
                {
                    UserId = userId,
                    TagId = tagId,
                    ReferableKind = kind,
                    ReferableId = referableId
                },
                cancellationToken);
        }

        var existingBuckets = await ctx.BucketAssignments
            .Where(x => x.UserId == userId && x.ReferableKind == kind && x.ReferableId == referableId)
            .ToListAsync(cancellationToken);
        ctx.BucketAssignments.RemoveRange(existingBuckets);

        foreach (var bucketId in distinctBuckets)
        {
            await ctx.BucketAssignments.AddAsync(
                new BucketAssignment
                {
                    UserId = userId,
                    BucketId = bucketId,
                    ReferableKind = kind,
                    ReferableId = referableId
                },
                cancellationToken);
        }

        return (true, null);
    }

    public async Task RemoveTaxonomyForReferableAsync(
        AppDbContext ctx,
        Guid userId,
        ReferableKind kind,
        Guid referableId,
        CancellationToken cancellationToken = default)
    {
        var tagRows = await ctx.TagAssignments
            .Where(x => x.UserId == userId && x.ReferableKind == kind && x.ReferableId == referableId)
            .ToListAsync(cancellationToken);
        ctx.TagAssignments.RemoveRange(tagRows);

        var bucketRows = await ctx.BucketAssignments
            .Where(x => x.UserId == userId && x.ReferableKind == kind && x.ReferableId == referableId)
            .ToListAsync(cancellationToken);
        ctx.BucketAssignments.RemoveRange(bucketRows);
    }

    public async Task RemoveTagAssignmentsAsync(
        AppDbContext ctx,
        Guid userId,
        Guid tagId,
        CancellationToken cancellationToken = default)
    {
        var rows = await ctx.TagAssignments
            .Where(x => x.UserId == userId && x.TagId == tagId)
            .ToListAsync(cancellationToken);
        ctx.TagAssignments.RemoveRange(rows);
    }

    public async Task RemoveBucketAssignmentsAsync(
        AppDbContext ctx,
        Guid userId,
        Guid bucketId,
        CancellationToken cancellationToken = default)
    {
        var rows = await ctx.BucketAssignments
            .Where(x => x.UserId == userId && x.BucketId == bucketId)
            .ToListAsync(cancellationToken);
        ctx.BucketAssignments.RemoveRange(rows);
    }

    private static ErTaxonomyDto EmptyTaxonomy() => new() { Tags = [], Buckets = [] };
}
