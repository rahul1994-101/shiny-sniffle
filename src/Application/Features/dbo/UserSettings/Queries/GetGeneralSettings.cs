using FluentValidation;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.dbo.UserSettings.Queries;

public sealed record GetGeneralSettingsRequest(Guid UserId)
    : IQuery<GetGeneralSettingsResponse>;

public sealed class GetGeneralSettingsResponse : GeneralSettingsDto
{
}

public sealed class GetGeneralSettingsRequestValidator : AbstractValidator<GetGeneralSettingsRequest>
{
    public GetGeneralSettingsRequestValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User Id is required.");
    }
}

public sealed class GetGeneralSettingsRequestHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<GetGeneralSettingsRequest, GetGeneralSettingsResponse>
{
    public async ValueTask<Result<GetGeneralSettingsResponse>> HandleAsync(GetGeneralSettingsRequest request, CancellationToken cancellationToken = default)
    {
        var result = new Result<GetGeneralSettingsResponse>();

        #region # Execute

        var profile = await GetGeneralSettingsAsync(request.UserId, cancellationToken);

        #endregion

        #region # Handle Result

        if (profile is null)
        {
            result.Failure(ErrorCode.NotFound, "User not found.");
        }
        else
        {
            result.Success(profile.AsResponse<GetGeneralSettingsResponse>());
        }

        #endregion

        return result;
    }

    #region # Private Helpers

    private async Task<GeneralSettingsDto?> GetGeneralSettingsAsync(Guid userId, CancellationToken cancellationToken)
    {
        await using var ctx = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var user = await ctx.Users
            .AsNoTracking()
            .Where(x =>
                x.Id == userId &&
                x.IsActive &&
                !x.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);

        return user is null ? null : GeneralSettingsDto.FromEntity(user);
    }

    #endregion
}
