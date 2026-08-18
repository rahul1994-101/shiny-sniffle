using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Workspace.Tags;

public sealed class TagRepository(
    IDbContextFactory<AppDbContext> dbContextFactory,
    SharedRepository sharedRepo)
{
    public async Task<IReadOnlyList<TagDto>> GetAllTagsByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        await using var ctx = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var rows = await ctx.Tags
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.IsActive && !x.IsDeleted)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        return rows.ConvertAll(TagDto.FromEntity);
    }

    public async Task<TagDto?> GetTagByIdAsync(Guid userId, Guid tagId, CancellationToken cancellationToken = default)
    {
        await using var ctx = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var row = await FindActiveAsync(ctx, userId, tagId, asNoTracking: true, cancellationToken);
        return row is null ? null : TagDto.FromEntity(row);
    }

    public async Task<(TagDto? Saved, string? Error, bool NotFound)> SaveAsync(
        Guid userId,
        SaveTagDto dto,
        Guid updatedBy,
        CancellationToken cancellationToken = default)
    {
        await using var ctx = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var name = TagMapping.NormalizeName(dto.Name);
        var color = CatalogFieldRules.NormalizeColor(dto.Color);
        var context = CatalogFieldRules.NormalizeContext(dto.Context);
        var now = DateTime.UtcNow;

        Tag? existing = null;
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
            EntityRefs.Kind.Tag,
            dto.Alias,
            dto.Id,
            existing?.Alias,
            name,
            secondarySource: null,
            cancellationToken);

        if (await IsAliasTakenAsync(ctx, userId, alias, dto.Id, cancellationToken))
        {
            return (null, "A tag with this alias already exists.", false);
        }

        Tag entity;
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
            entity = new Tag
            {
                UserId = userId,
                Name = name,
                Alias = alias,
                Color = color,
                Context = context,
                CreatedBy = updatedBy,
                UpdatedBy = updatedBy,
                CreatedAt = now,
                UpdatedAt = now
            };
            await ctx.Tags.AddAsync(entity, cancellationToken);
        }

        await ctx.SaveChangesAsync(cancellationToken);
        return (TagDto.FromEntity(entity), null, false);
    }

    public async Task<bool> DeleteAsync(
        Guid userId,
        Guid tagId,
        Guid updatedBy,
        CancellationToken cancellationToken = default)
    {
        await using var ctx = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var entity = await FindActiveAsync(ctx, userId, tagId, asNoTracking: false, cancellationToken);
        if (entity is null)
        {
            return false;
        }

        entity.IsDeleted = true;
        entity.IsActive = false;
        entity.UpdatedBy = updatedBy;
        entity.UpdatedAt = DateTime.UtcNow;

        await sharedRepo.RemoveTagAssignmentsAsync(ctx, userId, tagId, cancellationToken);
        await ctx.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static Task<bool> IsAliasTakenAsync(
        AppDbContext ctx,
        Guid userId,
        string alias,
        Guid? excludeId,
        CancellationToken cancellationToken) =>
        ctx.Tags.AnyAsync(
            x =>
                x.UserId == userId &&
                !x.IsDeleted &&
                x.Alias == alias &&
                x.Id != excludeId,
            cancellationToken);

    private static async Task<Tag?> FindActiveAsync(
        AppDbContext ctx,
        Guid userId,
        Guid tagId,
        bool asNoTracking,
        CancellationToken cancellationToken)
    {
        var query = ctx.Tags.Where(x =>
            x.Id == tagId &&
            x.UserId == userId &&
            x.IsActive &&
            !x.IsDeleted);

        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync(cancellationToken);
    }
}
