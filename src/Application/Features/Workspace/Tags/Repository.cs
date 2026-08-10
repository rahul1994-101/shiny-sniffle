using Application.Features.Shared;
using Infrastructure.Persistence;
using Infrastructure.Persistence.workspace;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.workspace.Tags;

public sealed class TagRepository(
    IDbContextFactory<AppDbContext> dbContextFactory,
    ErTaxonomyRepository taxonomyRepo)
{
    public async Task<IReadOnlyList<TagDto>> ListAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        await using var ctx = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var rows = await ctx.Tags
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.IsActive && !x.IsDeleted)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);

        return rows.ConvertAll(TagMapping.ToDto);
    }

    public async Task<(TagDto? Saved, string? Error, bool NotFound)> SaveAsync(
        Guid userId,
        SaveTagDto dto,
        Guid updatedBy,
        CancellationToken cancellationToken = default)
    {
        await using var ctx = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var name = TagMapping.NormalizeName(dto.Name);
        var color = TagMapping.NormalizeColor(dto.Color);
        var now = DateTime.UtcNow;

        if (await IsNameTakenAsync(ctx, userId, name, dto.Id, cancellationToken))
        {
            return (null, "A tag with this name already exists.", false);
        }

        Tag entity;
        if (dto.Id is { } id)
        {
            var existing = await FindActiveAsync(ctx, userId, id, track: true, cancellationToken);
            if (existing is null)
            {
                return (null, null, true);
            }

            entity = existing;
            entity.Name = name;
            entity.Color = color;
            entity.UpdatedBy = updatedBy;
            entity.UpdatedAt = now;
        }
        else
        {
            var sortOrder = await ctx.Tags
                .Where(x => x.UserId == userId && !x.IsDeleted)
                .Select(x => (int?)x.SortOrder)
                .MaxAsync(cancellationToken) ?? 0;

            entity = new Tag
            {
                UserId = userId,
                Name = name,
                Color = color,
                SortOrder = sortOrder + 10,
                CreatedBy = updatedBy,
                UpdatedBy = updatedBy,
                CreatedAt = now,
                UpdatedAt = now
            };
            await ctx.Tags.AddAsync(entity, cancellationToken);
        }

        await ctx.SaveChangesAsync(cancellationToken);
        return (TagMapping.ToDto(entity), null, false);
    }

    public async Task<bool> SoftDeleteAsync(
        Guid userId,
        Guid tagId,
        Guid updatedBy,
        CancellationToken cancellationToken = default)
    {
        await using var ctx = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var entity = await FindActiveAsync(ctx, userId, tagId, track: true, cancellationToken);
        if (entity is null)
        {
            return false;
        }

        entity.IsDeleted = true;
        entity.IsActive = false;
        entity.UpdatedBy = updatedBy;
        entity.UpdatedAt = DateTime.UtcNow;

        await taxonomyRepo.RemoveAssignmentsForTagAsync(ctx, userId, tagId, cancellationToken);
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
        return await ctx.Tags.AnyAsync(
            x =>
                x.UserId == userId &&
                !x.IsDeleted &&
                x.Name.ToLower() == lower &&
                x.Id != excludeId,
            cancellationToken);
    }

    private static async Task<Tag?> FindActiveAsync(
        AppDbContext ctx,
        Guid userId,
        Guid tagId,
        bool track,
        CancellationToken cancellationToken)
    {
        var query = ctx.Tags.Where(x =>
            x.Id == tagId &&
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
