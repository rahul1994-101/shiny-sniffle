namespace Application.Models;

/// <summary>
/// Chat thread agent selection for UI and use cases.
/// Values match <see cref="Infrastructure.Persistence.Shared.ChatThreadAgent"/> / DB column <c>chatAgent</c>.
/// </summary>
public enum ChatAgent
{
    Assistant = 0,
    Email = 1
}
