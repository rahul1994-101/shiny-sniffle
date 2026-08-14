using FluentValidation;

namespace Application.Features.Dbo.Users.Queries;

public sealed record GetGeneralSettingsByUserIdRequest(Guid UserId)
    : IQuery<GetGeneralSettingsByUserIdResponse>;

public sealed class GetGeneralSettingsByUserIdResponse : GeneralSettingsDto
{
}

public sealed class GetGeneralSettingsByUserIdRequestValidator : AbstractValidator<GetGeneralSettingsByUserIdRequest>
{
    public GetGeneralSettingsByUserIdRequestValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User Id is required.");
    }
}

public sealed class GetGeneralSettingsByUserIdRequestHandler(UserRepository userRepo)
    : IRequestHandler<GetGeneralSettingsByUserIdRequest, GetGeneralSettingsByUserIdResponse>
{
    public async ValueTask<Result<GetGeneralSettingsByUserIdResponse>> HandleAsync(GetGeneralSettingsByUserIdRequest request, CancellationToken cancellationToken = default)
    {
        var result = new Result<GetGeneralSettingsByUserIdResponse>();

        #region # Execute

        var profile = await userRepo.GetGeneralSettingsByUserIdAsync(request.UserId, cancellationToken);

        #endregion

        #region # Handle Result

        if (profile is null)
        {
            result.Failure(ErrorCode.NotFound, "User not found.");
        }
        else
        {
            result.Success(profile.AsResponse<GetGeneralSettingsByUserIdResponse>());
        }

        #endregion

        return result;
    }
}
