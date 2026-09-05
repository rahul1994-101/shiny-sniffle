using Application.Features.Workspace.EmailAccounts;

namespace Application.Features.Shared;

/// <summary>Identity resolution payload for a workspace entity reference.</summary>
public sealed record EntityRefId(EntityRefs.Kind Kind, Guid Id);

/// <summary>Resolved <c>@kind:alias</c> tokens from one chat message — context for the LLM plus mailbox tool defaults.</summary>
public sealed class EntityRefMentionResolution
{
    public string? ContextBlock { get; init; }

    /// <summary>Pre-resolved mailbox for this turn (mention or default when requested).</summary>
    public MailboxAccountContext? DefaultMailboxAccount { get; init; }

    /// <summary>True when more than one mailbox was mentioned — tools must receive <c>mailbox_alias</c>.</summary>
    public bool RequireMailboxAlias { get; init; }

    public string? DefaultMailboxAlias => DefaultMailboxAccount?.Alias;
}

public sealed class TagRefDto
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Alias { get; init; } = string.Empty;

    public string EntityRef => EntityRefs.Format(EntityRefs.Kind.Tag, Alias);

    public string? Color { get; init; }

    public string? Context { get; init; }
}

public sealed class BucketRefDto
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Alias { get; init; } = string.Empty;

    public string EntityRef => EntityRefs.Format(EntityRefs.Kind.Bucket, Alias);

    public string? Color { get; init; }

    public string? Context { get; init; }
}

public sealed class ErTaxonomyDto
{
    public IReadOnlyList<TagRefDto> Tags { get; init; } = [];

    public IReadOnlyList<BucketRefDto> Buckets { get; init; } = [];
}

/// <summary>Lightweight row for the <c>@kind:alias</c> mention picker (no taxonomy).</summary>
public sealed class EntityRefMentionItemDto
{
    public EntityRefs.Kind Kind { get; init; }

    public string Alias { get; init; } = string.Empty;

    public string PrimaryLabel { get; init; } = string.Empty;

    public string? SecondaryLabel { get; init; }

    public string? TooltipText { get; init; }

    public string? AvatarText { get; init; }
}
