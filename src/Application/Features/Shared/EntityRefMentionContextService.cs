using Application.Features.Workspace.Buckets;
using Application.Features.Workspace.Contacts;
using Application.Features.Workspace.EmailAccounts;
using Application.Features.Workspace.Tags;

namespace Application.Features.Shared;

/// <summary>Resolves <c>@kind:alias</c> tokens in chat text into agent-readable context.</summary>
public sealed class EntityRefMentionContextService(
    EntityRefResolver entityRefResolver,
    ContactRepository contactRepo,
    EmailAccountRepository emailAccountRepo,
    TagRepository tagRepo,
    BucketRepository bucketRepo)
{
    public async Task<string?> BuildContextBlockAsync(
        Guid userId,
        string message,
        CancellationToken cancellationToken = default)
    {
        var handles = EntityRefMentions.ExtractFromText(message);
        if (handles.Count == 0)
        {
            return null;
        }

        var lines = new List<string> { "## Referenced entities" };

        foreach (var handle in handles)
        {
            if (!EntityRefs.TryParse(handle, out var kind, out var alias))
            {
                lines.Add($"- `{handle}`: could not parse reference.");
                continue;
            }

            var resolve = await entityRefResolver.TryResolveIdAsync(userId, kind, alias, cancellationToken);
            if (!resolve.Success)
            {
                lines.Add($"- `{handle}`: {resolve.Error}");
                continue;
            }

            var line = kind switch
            {
                EntityRefs.Kind.Contact => await FormatContactAsync(userId, resolve.Id, handle, cancellationToken),
                EntityRefs.Kind.Mailbox => await FormatMailboxAsync(userId, resolve.Id, handle, cancellationToken),
                EntityRefs.Kind.Tag => await FormatTagAsync(userId, resolve.Id, handle, cancellationToken),
                EntityRefs.Kind.Bucket => await FormatBucketAsync(userId, resolve.Id, handle, cancellationToken),
                _ => $"- `{handle}`: resolved (id {resolve.Id:D})."
            };

            lines.Add(line);
        }

        return lines.Count > 1 ? string.Join('\n', lines) : null;
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

    private async Task<string> FormatMailboxAsync(
        Guid userId,
        Guid mailboxId,
        string handle,
        CancellationToken cancellationToken)
    {
        var account = await emailAccountRepo.GetEmailAccountByIdAsync(userId, mailboxId, cancellationToken);
        if (account is null)
        {
            return $"- `{handle}`: mailbox not found.";
        }

        var defaultLabel = account.IsDefault ? "; default mailbox" : string.Empty;
        return $"- `{handle}` (mailbox): {account.EmailAddress} via {account.ProviderName}{defaultLabel}.";
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
