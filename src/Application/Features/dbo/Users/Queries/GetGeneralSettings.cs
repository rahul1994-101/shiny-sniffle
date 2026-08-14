using FluentValidation;

namespace Application.Features.dbo.Users.Queries;

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

public sealed class GetGeneralSettingsRequestHandler(UserRepository userRepo)
    : IRequestHandler<GetGeneralSettingsRequest, GetGeneralSettingsResponse>
{
    public async ValueTask<Result<GetGeneralSettingsResponse>> HandleAsync(GetGeneralSettingsRequest request, CancellationToken cancellationToken = default)
    {
        var result = new Result<GetGeneralSettingsResponse>();

        #region # Execute

        var profile = await userRepo.GetGeneralSettingsAsync(request.UserId, cancellationToken);

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
}
