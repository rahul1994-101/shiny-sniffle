using Infrastructure.Persistence;
using Infrastructure.Persistence.Shared;
using Infrastructure.Persistence.workspace;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Shared;

public sealed class ErTaxonomyRepository
{
    public async Task<IReadOnlyDictionary<Guid, ErTaxonomyDto>> LoadForReferablesAsync(
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
                ctx.Tags.AsNoTracking().Where(t => t.UserId == userId && !t.IsDeleted && t.IsActive),
                a => a.TagId,
                t => t.Id,
                (a, t) => new { a.ReferableId, Tag = t })
            .ToListAsync(cancellationToken);

        var bucketRows = await ctx.BucketMembers
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.ReferableKind == kind && referableIds.Contains(x.ReferableId))
            .Join(
                ctx.Buckets.AsNoTracking().Where(b => b.UserId == userId && !b.IsDeleted && b.IsActive),
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
                    .Select(x => new TagRefDto { Id = x.Tag.Id, Name = x.Tag.Name, Color = x.Tag.Color })
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
                    .Select(x => new BucketRefDto { Id = x.Bucket.Id, Name = x.Bucket.Name })
                    .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                    .ToList()
            };
        }

        return map;
    }

    public async Task<(bool Ok, string? Error)> SyncAsync(
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
            var validCount = await ctx.Tags.CountAsync(
                t => t.UserId == userId && !t.IsDeleted && t.IsActive && distinctTags.Contains(t.Id),
                cancellationToken);

            if (validCount != distinctTags.Count)
            {
                return (false, "One or more tags are invalid.");
            }
        }

        if (distinctBuckets.Count > 0)
        {
            var validCount = await ctx.Buckets.CountAsync(
                b => b.UserId == userId && !b.IsDeleted && b.IsActive && distinctBuckets.Contains(b.Id),
                cancellationToken);

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
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    TagId = tagId,
                    ReferableKind = kind,
                    ReferableId = referableId
                },
                cancellationToken);
        }

        var existingBuckets = await ctx.BucketMembers
            .Where(x => x.UserId == userId && x.ReferableKind == kind && x.ReferableId == referableId)
            .ToListAsync(cancellationToken);
        ctx.BucketMembers.RemoveRange(existingBuckets);

        foreach (var bucketId in distinctBuckets)
        {
            await ctx.BucketMembers.AddAsync(
                new BucketMember
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    BucketId = bucketId,
                    ReferableKind = kind,
                    ReferableId = referableId
                },
                cancellationToken);
        }

        return (true, null);
    }

    public async Task RemoveForReferableAsync(
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

        var bucketRows = await ctx.BucketMembers
            .Where(x => x.UserId == userId && x.ReferableKind == kind && x.ReferableId == referableId)
            .ToListAsync(cancellationToken);
        ctx.BucketMembers.RemoveRange(bucketRows);
    }

    public async Task RemoveAssignmentsForTagAsync(
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

    public async Task RemoveMembersForBucketAsync(
        AppDbContext ctx,
        Guid userId,
        Guid bucketId,
        CancellationToken cancellationToken = default)
    {
        var rows = await ctx.BucketMembers
            .Where(x => x.UserId == userId && x.BucketId == bucketId)
            .ToListAsync(cancellationToken);
        ctx.BucketMembers.RemoveRange(rows);
    }

    private static ErTaxonomyDto EmptyTaxonomy() => new() { Tags = [], Buckets = [] };
}
