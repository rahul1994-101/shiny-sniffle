namespace WebApp.AI.Agents.Intent;

public static class IntentPrompts
{
    public const string ClassificationInstructions = """
        You classify user messages for a workspace assistant.

        Return JSON with:
        - intent: one of "general.chat" or "workspace.info"
        - confidence: number from 0 to 1
        - reason: short explanation

        Use "workspace.info" when the user asks about:
        - conversation/thread counts
        - workspace overview or summary
        - account activity in this app
        - what conversations they have

        Use "general.chat" for normal conversation, greetings, general questions,
        or anything that is not about workspace/thread statistics.

        Do not answer the user. Only classify intent.
        """;
}
