using FluentValidation;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.chat.ChatThreads.Queries;

public sealed record GetChatThreadsByUserIdRequest(Guid UserId)
    : IQuery<GetChatThreadsByUserIdResponse>;

public sealed class GetChatThreadsByUserIdResponse
{
    public List<ChatThreadDto> Threads { get; init; } = [];
}

public sealed class GetChatThreadsByUserIdRequestValidator : AbstractValidator<GetChatThreadsByUserIdRequest>
{
    public GetChatThreadsByUserIdRequestValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User Id is required.");
    }
}

public sealed class GetChatThreadsByUserIdRequestHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<GetChatThreadsByUserIdRequest, GetChatThreadsByUserIdResponse>
{
    public async ValueTask<Result<GetChatThreadsByUserIdResponse>> HandleAsync(GetChatThreadsByUserIdRequest request, CancellationToken cancellationToken = default)
    {
        var result = new Result<GetChatThreadsByUserIdResponse>();

        #region # Execute

        var threads = await GetActiveByUserIdAsync(request.UserId, cancellationToken);

        #endregion

        #region # Handle Result

        result.Success(new GetChatThreadsByUserIdResponse { Threads = threads });

        #endregion

        return result;
    }

    #region # Private Helpers

    private async Task<List<ChatThreadDto>> GetActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        await using var ctx = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var threads = await ctx.ChatThreads
            .AsNoTracking()
            .Where(x =>
                x.UserId == userId &&
                x.IsActive &&
                !x.IsDeleted)
            .OrderByDescending(x => x.UpdatedAt)
            .ToListAsync(cancellationToken);

        return ChatThreadDto.FromEntities(threads);
    }

    #endregion
}
