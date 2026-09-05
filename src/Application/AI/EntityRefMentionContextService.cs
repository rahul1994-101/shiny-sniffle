using Application.Features.Workspace.Buckets;
using Application.Features.Workspace.Contacts;
using Application.Features.Workspace.EmailAccounts;
using Application.Features.Workspace.Tags;

namespace Application.AI;

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
        var resolvedMailboxCount = 0;

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
                if (!outcome.HasError)
                {
                    resolvedMailboxCount++;
                    defaultMailboxAccount ??= outcome.Payload;
                    lines!.Add(FormatMailboxLine(handle, outcome.Payload!));
                }
                else
                {
                    lines!.Add($"- `{handle}`: {outcome.FirstErrorMessage}");
                }

                continue;
            }

            var resolve = await workspaceRefs.TryResolveIdAsync(userId, kind, alias, cancellationToken);
            if (resolve.HasError)
            {
                lines!.Add($"- `{handle}`: {resolve.FirstErrorMessage}");
                continue;
            }

            var refId = resolve.Payload!;
            var line = refId.Kind switch
            {
                EntityRefs.Kind.Contact => await FormatContactAsync(userId, refId.Id, handle, cancellationToken),
                EntityRefs.Kind.Tag => await FormatTagAsync(userId, refId.Id, handle, cancellationToken),
                EntityRefs.Kind.Bucket => await FormatBucketAsync(userId, refId.Id, handle, cancellationToken),
                _ => $"- `{handle}`: resolved (id {refId.Id:D})."
            };

            lines!.Add(line);
        }

        var requireMailboxAlias = resolvedMailboxCount > 1;
        if (requireMailboxAlias)
        {
            defaultMailboxAccount = null;
            lines!.Add("- Multiple mailboxes mentioned. Pass mailbox_alias on every mailbox tool call this turn; do not assume a default.");
        }
        else if (resolveDefaultMailbox && defaultMailboxAccount is null)
        {
            var defaultOutcome = await workspaceRefs.TryResolveMailboxAsync(userId, null, cancellationToken);
            if (!defaultOutcome.HasError)
            {
                defaultMailboxAccount = defaultOutcome.Payload;
            }
        }

        return new EntityRefMentionResolution
        {
            ContextBlock = lines is { Count: > 1 } ? string.Join('\n', lines) : null,
            DefaultMailboxAccount = defaultMailboxAccount,
            RequireMailboxAlias = requireMailboxAlias
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
