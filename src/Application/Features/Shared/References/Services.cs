using Application.Features.Workspace.Buckets;
using Application.Features.Workspace.Contacts;
using Application.Features.Workspace.EmailAccounts;
using Application.Features.Workspace.Tags;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Shared;

/// <summary>
/// Single workspace entry point for entity reference resolution — identity (<c>kind:alias</c> → id) and action (ref → runtime context).
/// </summary>
public sealed class WorkspaceReferenceService(IDbContextFactory<AppDbContext> dbContextFactory, MailboxAccountResolver mailboxAccountResolver)
{
    #region # Identity

    public async Task<EntityRefResolveResult> TryResolveIdAsync(Guid userId, string entityRef, CancellationToken cancellationToken = default)
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

    public async Task<EntityRefResolveResult> TryResolveIdAsync(Guid userId, EntityRefs.Kind kind, string alias, CancellationToken cancellationToken = default)
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

    public async Task<EntityRefFormatResult> TryFormatAsync(Guid userId, EntityRefs.Kind kind, Guid id, CancellationToken cancellationToken = default)
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

    #endregion

    #region # Mailbox

    public async Task<bool> IsMailboxConfiguredAsync(Guid userId, string? mailboxRef = null, CancellationToken cancellationToken = default)
    {
        var outcome = await TryResolveMailboxAsync(userId, mailboxRef, cancellationToken);
        return outcome.IsSuccess;
    }

    public Task<MailboxResult<MailboxAccountContext>> TryResolveMailboxAsync(Guid userId, string? mailboxRef = null, CancellationToken cancellationToken = default)
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

/// <summary>
/// Parses <c>@kind:alias</c> tokens once per message and builds LLM context from <see cref="WorkspaceReferenceService"/>.
/// </summary>
public sealed class EntityRefMentionContextService(
    WorkspaceReferenceService workspaceRefs,
    ContactRepository contactRepo,
    TagRepository tagRepo,
    BucketRepository bucketRepo)
{
    /// <summary>
    /// Single pass over message mentions — LLM context block and pre-resolved default mailbox for tools.
    /// </summary>
    public async Task<EntityRefMentionResolution> ResolveAsync(
        Guid userId,
        string message,
        bool resolveDefaultMailbox = false,
        CancellationToken cancellationToken = default)
    {
        var handles = EntityRefMentions.ExtractFromText(message);
        var lines = handles.Count == 0 ? null : new List<string> { "## Referenced entities" };
        MailboxAccountContext? defaultMailboxAccount = null;

        foreach (var handle in handles)
        {
            if (!EntityRefs.TryParse(handle, out var kind, out var alias))
            {
                lines!.Add($"- `{handle}`: could not parse reference.");
                continue;
            }

            if (kind == EntityRefs.Kind.Mailbox)
            {
                var outcome = await workspaceRefs.TryResolveMailboxAsync(userId, handle, cancellationToken);
                if (outcome.IsSuccess)
                {
                    defaultMailboxAccount ??= outcome.Account;
                    lines!.Add(FormatMailboxLine(handle, outcome.Account!));
                }
                else
                {
                    lines!.Add($"- `{handle}`: {outcome.Error}");
                }

                continue;
            }

            var resolve = await workspaceRefs.TryResolveIdAsync(userId, kind, alias, cancellationToken);
            if (!resolve.Success)
            {
                lines!.Add($"- `{handle}`: {resolve.Error}");
                continue;
            }

            var line = kind switch
            {
                EntityRefs.Kind.Contact => await FormatContactAsync(userId, resolve.Id, handle, cancellationToken),
                EntityRefs.Kind.Tag => await FormatTagAsync(userId, resolve.Id, handle, cancellationToken),
                EntityRefs.Kind.Bucket => await FormatBucketAsync(userId, resolve.Id, handle, cancellationToken),
                _ => $"- `{handle}`: resolved (id {resolve.Id:D})."
            };

            lines!.Add(line);
        }

        if (resolveDefaultMailbox && defaultMailboxAccount is null)
        {
            var defaultOutcome = await workspaceRefs.TryResolveMailboxAsync(userId, null, cancellationToken);
            if (defaultOutcome.IsSuccess)
            {
                defaultMailboxAccount = defaultOutcome.Account;
            }
        }

        return new EntityRefMentionResolution
        {
            ContextBlock = lines is { Count: > 1 } ? string.Join('\n', lines) : null,
            DefaultMailboxAccount = defaultMailboxAccount
        };
    }

    private static string FormatMailboxLine(string handle, MailboxAccountContext account)
    {
        var defaultLabel = account.IsDefault ? "; default mailbox" : string.Empty;
        return
            $"- `{handle}` (mailbox): {account.EmailAddress} via {account.ProviderName}{defaultLabel}. " +
            $"Use mailbox_alias `{account.Alias}` on all mailbox tool calls this turn unless the user names another account.";
    }

    private async Task<string> FormatContactAsync(
        Guid userId,
        Guid contactId,
        string handle,
        CancellationToken cancellationToken)
    {
        var contact = await contactRepo.GetContactByIdAsync(userId, contactId, cancellationToken);
        if (contact is null)
        {
            return $"- `{handle}`: contact not found.";
        }

        var details = new List<string> { contact.ListLabel };
        if (!string.IsNullOrWhiteSpace(contact.Email))
        {
            details.Add($"email {contact.Email}");
        }

        if (!string.IsNullOrWhiteSpace(contact.Phone))
        {
            details.Add($"phone {contact.Phone}");
        }

        if (!string.IsNullOrWhiteSpace(contact.Context))
        {
            details.Add($"notes: {contact.Context.Trim()}");
        }

        return $"- `{handle}` (contact): {string.Join("; ", details)}.";
    }

    private async Task<string> FormatTagAsync(
        Guid userId,
        Guid tagId,
        string handle,
        CancellationToken cancellationToken)
    {
        var tag = await tagRepo.GetTagByIdAsync(userId, tagId, cancellationToken);
        if (tag is null)
        {
            return $"- `{handle}`: tag not found.";
        }

        return $"- `{handle}` (tag): {FormatCatalogDetails(tag.Name, tag.Color, tag.Context)}.";
    }

    private async Task<string> FormatBucketAsync(
        Guid userId,
        Guid bucketId,
        string handle,
        CancellationToken cancellationToken)
    {
        var bucket = await bucketRepo.GetBucketByIdAsync(userId, bucketId, cancellationToken);
        if (bucket is null)
        {
            return $"- `{handle}`: bucket not found.";
        }

        return $"- `{handle}` (bucket): {FormatCatalogDetails(bucket.Name, bucket.Color, bucket.Context)}.";
    }

    private static string FormatCatalogDetails(string name, string? color, string? context)
    {
        var details = new List<string> { name };

        if (!string.IsNullOrWhiteSpace(color))
        {
            details.Add($"color {color.Trim()}");
        }

        if (!string.IsNullOrWhiteSpace(context))
        {
            details.Add($"notes: {context.Trim()}");
        }

        return string.Join("; ", details);
    }
}
