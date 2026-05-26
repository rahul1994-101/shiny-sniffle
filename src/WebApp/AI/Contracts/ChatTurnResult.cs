namespace WebApp.AI.Contracts;

public sealed class ChatTurnResult
{
    public string AssistantContent { get; init; } = string.Empty;

    public string Intent { get; init; } = string.Empty;

    public string Handler { get; init; } = string.Empty;

    public string ModelDeployment { get; init; } = string.Empty;
}
