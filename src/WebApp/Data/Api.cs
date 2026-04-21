using WebApp.Models;

namespace WebApp.Data;

public static class Api
{
    public static WebApplication MapLangChainApi(this WebApplication app)
    {
        var g = app.MapGroup("/api/langchain").WithTags("LangChain");

        g.MapGet("/health", () => Results.Ok(new { status = "ok", at = DateTimeOffset.UtcNow }))
            .WithName("LangChainGatewayHealth");

        g.MapPost("/invoke", (SendChatRequestDTO body) =>
            {
                if (string.IsNullOrWhiteSpace(body.Message))
                {
                    return Results.BadRequest(new { error = "message is required" });
                }

                var reply = ChatMocks.AssistantReply(body.Message);
                return Results.Ok(new SendChatResponseDTO { Reply = reply });
            })
            .WithName("LangChainInvoke");

        return app;
    }
}
