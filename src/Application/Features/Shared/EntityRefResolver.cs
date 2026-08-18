using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Shared;

/// <summary>
/// Resolves workspace ER handles (<c>kind:alias</c>) to stable row IDs at AI/tool boundaries.
/// Persisted links, FKs, and workflow state must store <see cref="Guid"/> IDs — not alias strings.
/// </summary>
public sealed class EntityRefResolver(IDbContextFactory<AppDbContext> dbContextFactory)
{
    public async Task<EntityRefResolveResult> TryResolveIdAsync(
        Guid userId,
        string entityRef,
        CancellationToken cancellationToken = default)
    {
        if (!EntityRefs.TryParse(entityRef, out var kind, out var alias))
        {
            return EntityRefResolveResult.InvalidRef(
                string.IsNullOrWhiteSpace(entityRef)
                    ? "Entity reference is required."
                    : $"Could not parse entity reference \"{entityRef.Trim()}\". Expected kind:alias (e.g. contact:sarah).");
        }

        return await TryResolveIdAsync(userId, kind, alias, cancellationToken);
    }

    public async Task<EntityRefResolveResult> TryResolveIdAsync(
        Guid userId,
        EntityRefs.Kind kind,
        string alias,
        CancellationToken cancellationToken = default)
    {
        var normalizedAlias = EntityAliasRules.SlugifyOptional(alias);
        if (normalizedAlias is null)
        {
            return EntityRefResolveResult.InvalidRef("Alias is required.");
        }

        await using var ctx = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var id = kind switch
        {
            EntityRefs.Kind.Contact => await ResolveContactIdAsync(ctx, userId, normalizedAlias, cancellationToken),
            EntityRefs.Kind.Mailbox => await ResolveMailboxIdAsync(ctx, userId, normalizedAlias, cancellationToken),
            EntityRefs.Kind.Tag => await ResolveTagIdAsync(ctx, userId, normalizedAlias, cancellationToken),
            EntityRefs.Kind.Bucket => await ResolveBucketIdAsync(ctx, userId, normalizedAlias, cancellationToken),
            _ => (Guid?)null
        };

        if (id is null)
        {
            return EntityRefResolveResult.NotFound(
                $"No active {EntityRefResolverCopy.KindLabel(kind)} found for {EntityRefs.Format(kind, normalizedAlias)}.");
        }

        return EntityRefResolveResult.Found(kind, id.Value);
    }

    /// <summary>Hydrates a stored ID back to a canonical <c>kind:alias</c> handle for AI prompts and UI chips.</summary>
    public async Task<EntityRefFormatResult> TryFormatAsync(
        Guid userId,
        EntityRefs.Kind kind,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            return EntityRefFormatResult.Invalid("Id is required.");
        }

        await using var ctx = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var alias = kind switch
        {
            EntityRefs.Kind.Contact => await FormatContactAliasAsync(ctx, userId, id, cancellationToken),
            EntityRefs.Kind.Mailbox => await FormatMailboxAliasAsync(ctx, userId, id, cancellationToken),
            EntityRefs.Kind.Tag => await FormatTagAliasAsync(ctx, userId, id, cancellationToken),
            EntityRefs.Kind.Bucket => await FormatBucketAliasAsync(ctx, userId, id, cancellationToken),
            _ => null
        };

        if (alias is null)
        {
            return EntityRefFormatResult.NotFound(
                $"No active {EntityRefResolverCopy.KindLabel(kind)} found for id {id:D}.");
        }

        return EntityRefFormatResult.Found(EntityRefs.Format(kind, alias));
    }

    private static Task<Guid?> ResolveContactIdAsync(
        AppDbContext ctx,
        Guid userId,
        string alias,
        CancellationToken cancellationToken) =>
        ctx.Contacts
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.Alias == alias)
            .WhereActive()
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync(cancellationToken);

    private static Task<Guid?> ResolveMailboxIdAsync(
        AppDbContext ctx,
        Guid userId,
        string alias,
        CancellationToken cancellationToken) =>
        ctx.EmailAccounts
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.Alias == alias)
            .WhereActive()
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync(cancellationToken);

    private static Task<Guid?> ResolveTagIdAsync(
        AppDbContext ctx,
        Guid userId,
        string alias,
        CancellationToken cancellationToken) =>
        ctx.Tags
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.Alias == alias)
            .WhereActive()
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync(cancellationToken);

    private static Task<Guid?> ResolveBucketIdAsync(
        AppDbContext ctx,
        Guid userId,
        string alias,
        CancellationToken cancellationToken) =>
        ctx.Buckets
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.Alias == alias)
            .WhereActive()
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync(cancellationToken);

    private static Task<string?> FormatContactAliasAsync(
        AppDbContext ctx,
        Guid userId,
        Guid id,
        CancellationToken cancellationToken) =>
        ctx.Contacts
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.Id == id)
            .WhereActive()
            .Select(x => x.Alias)
            .FirstOrDefaultAsync(cancellationToken);

    private static Task<string?> FormatMailboxAliasAsync(
        AppDbContext ctx,
        Guid userId,
        Guid id,
        CancellationToken cancellationToken) =>
        ctx.EmailAccounts
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.Id == id)
            .WhereActive()
            .Select(x => x.Alias)
            .FirstOrDefaultAsync(cancellationToken);

    private static Task<string?> FormatTagAliasAsync(
        AppDbContext ctx,
        Guid userId,
        Guid id,
        CancellationToken cancellationToken) =>
        ctx.Tags
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.Id == id)
            .WhereActive()
            .Select(x => x.Alias)
            .FirstOrDefaultAsync(cancellationToken);

    private static Task<string?> FormatBucketAliasAsync(
        AppDbContext ctx,
        Guid userId,
        Guid id,
        CancellationToken cancellationToken) =>
        ctx.Buckets
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.Id == id)
            .WhereActive()
            .Select(x => x.Alias)
            .FirstOrDefaultAsync(cancellationToken);
}

public readonly record struct EntityRefResolveResult(
    bool Success,
    Guid Id,
    EntityRefs.Kind Kind,
    string? Error)
{
    public static EntityRefResolveResult Found(EntityRefs.Kind kind, Guid id) =>
        new(true, id, kind, null);

    public static EntityRefResolveResult InvalidRef(string error) =>
        new(false, Guid.Empty, default, error);

    public static EntityRefResolveResult NotFound(string error) =>
        new(false, Guid.Empty, default, error);
}

public readonly record struct EntityRefFormatResult(
    bool Success,
    string EntityRef,
    string? Error)
{
    public static EntityRefFormatResult Found(string entityRef) =>
        new(true, entityRef, null);

    public static EntityRefFormatResult Invalid(string error) =>
        new(false, string.Empty, error);

    public static EntityRefFormatResult NotFound(string error) =>
        new(false, string.Empty, error);
}

internal static class EntityRefResolverCopy
{
    internal static string KindLabel(EntityRefs.Kind kind) => kind switch
    {
        EntityRefs.Kind.Contact => "contact",
        EntityRefs.Kind.Mailbox => "mailbox",
        EntityRefs.Kind.Tag => "tag",
        EntityRefs.Kind.Bucket => "bucket",
        _ => "item"
    };
}
