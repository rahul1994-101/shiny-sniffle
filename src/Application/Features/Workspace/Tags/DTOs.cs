namespace Application.Features.Workspace.Tags;

public class TagDto
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Alias { get; init; } = string.Empty;

    public string EntityRef => EntityRefs.Format(EntityRefs.Kind.Tag, Alias);

    public string? Color { get; init; }

    public string? Context { get; init; }

    public static TagDto FromEntity(Tag entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        Alias = entity.Alias,
        Color = entity.Color,
        Context = entity.Context
    };

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
        Context = Context
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
