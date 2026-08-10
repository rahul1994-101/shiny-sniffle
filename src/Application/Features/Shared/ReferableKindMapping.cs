using Infrastructure.Persistence.Shared;

namespace Application.Features.Shared;

public static class ReferableKindMapping
{
    public static ReferableKind ToPersistence(EntityRefs.Kind kind) => kind switch
    {
        EntityRefs.Kind.Contact => ReferableKind.Contact,
        EntityRefs.Kind.Mailbox => ReferableKind.Mailbox,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
    };
}
