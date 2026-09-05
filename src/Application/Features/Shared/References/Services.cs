using Application.Features.Workspace.EmailAccounts;
using Infrastructure.Persistence;
using MediatR.Results;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Shared;

/// <summary>
/// Single workspace entry point for entity reference resolution — identity (<c>kind:alias</c> → id) and action (ref → runtime context).
/// </summary>
public sealed class WorkspaceReferenceService(IDbContextFactory<AppDbContext> dbContextFactory, MailboxAccountResolver mailboxAccountResolver)
{
    #region # Identity

    public Task<Result<EntityRefId>> TryResolveIdAsync(Guid userId, string entityRef, CancellationToken cancellationToken = default)
    {
        var result = new Result<EntityRefId>();
        if (!EntityRefs.TryParse(entityRef, out var kind, out var alias))
        {
            result.Failure(
                ErrorCode.BadRequest,
                string.IsNullOrWhiteSpace(entityRef)
                    ? "Entity reference is required."
                    : $"Could not parse entity reference \"{entityRef.Trim()}\". Expected kind:alias (e.g. contact:sarah).");
            return Task.FromResult(result);
        }

        return TryResolveIdAsync(userId, kind, alias, cancellationToken);
    }

    public async Task<Result<EntityRefId>> TryResolveIdAsync(Guid userId, EntityRefs.Kind kind, string alias, CancellationToken cancellationToken = default)
    {
        var result = new Result<EntityRefId>();
        var normalizedAlias = EntityAliasRules.SlugifyOptional(alias);
        if (normalizedAlias is null)
        {
            result.Failure(ErrorCode.BadRequest, "Alias is required.");
            return result;
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
            result.Failure(
                ErrorCode.NotFound,
                $"No active {EntityRefResolverCopy.KindLabel(kind)} found for {EntityRefs.Format(kind, normalizedAlias)}.");
            return result;
        }

        result.Success(new EntityRefId(kind, id.Value));
        return result;
    }

    public async Task<Result<string>> TryFormatAsync(Guid userId, EntityRefs.Kind kind, Guid id, CancellationToken cancellationToken = default)
    {
        var result = new Result<string>();
        if (id == Guid.Empty)
        {
            result.Failure(ErrorCode.BadRequest, "Id is required.");
            return result;
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
            result.Failure(
                ErrorCode.NotFound,
                $"No active {EntityRefResolverCopy.KindLabel(kind)} found for id {id:D}.");
            return result;
        }

        result.Success(EntityRefs.Format(kind, alias));
        return result;
    }

    #endregion

    #region # Mailbox

    public Task<Result<MailboxAccountContext>> TryResolveMailboxAsync(Guid userId, string? mailboxRef = null, CancellationToken cancellationToken = default)
    {
        return mailboxAccountResolver.TryResolveAccountAsync(userId, mailboxRef, cancellationToken);
    }

    #endregion

    #region # Identity internals

    private static Task<Guid?> ResolveContactIdAsync(
        AppDbContext ctx,
        Guid userId,
        string alias,
        CancellationToken cancellationToken) =>
        ctx.Contacts
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.Alias == alias)
            .WhereActiveAndNotDeleted()
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
            .WhereActiveAndNotDeleted()
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
            .WhereActiveAndNotDeleted()
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
            .WhereActiveAndNotDeleted()
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
            .WhereActiveAndNotDeleted()
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
            .WhereActiveAndNotDeleted()
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
            .WhereActiveAndNotDeleted()
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
            .WhereActiveAndNotDeleted()
            .Select(x => x.Alias)
            .FirstOrDefaultAsync(cancellationToken);

    #endregion
}
