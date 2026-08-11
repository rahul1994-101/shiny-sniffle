namespace Application.Features.workspace.Tags;

public class TagDto
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Alias { get; init; } = string.Empty;

    public string EntityRef => EntityRefs.Format(EntityRefs.Kind.Tag, Alias);

    public string? Color { get; init; }

    public string? Context { get; init; }

    public int SortOrder { get; init; }

    public TagRefDto AsRef() => new()
    {
        Id = Id,
        Name = Name,
        Alias = Alias,
        Color = Color,
        Context = Context
    };

    public T AsResponse<T>() where T : TagDto, new() => new()
    {
        Id = Id,
        Name = Name,
        Alias = Alias,
        Color = Color,
        Context = Context,
        SortOrder = SortOrder
    };
}

public sealed class SaveTagDto
{
    public Guid? Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string? Alias { get; init; }

    public string? Color { get; init; }

    public string? Context { get; init; }
}
