namespace Application.Features.workspace.Tags;

using Application.Features.Shared;

public class TagDto
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string? Color { get; init; }

    public int SortOrder { get; init; }

    public TagRefDto AsRef() => new() { Id = Id, Name = Name, Color = Color };
}

public sealed class SaveTagDto
{
    public Guid? Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string? Color { get; init; }
}
