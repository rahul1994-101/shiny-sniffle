using FluentValidation;


namespace WebApp.Features.UserSettings.Queries;

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

public sealed class GetGeneralSettingsRequestHandler(UserSettingsRepository userSettingsRepo, SharedRepository sharedRepo)
    : IRequestHandler<GetGeneralSettingsRequest, GetGeneralSettingsResponse>
{
    private readonly SharedRepository _sharedRepo = sharedRepo;


    public async ValueTask<Result<GetGeneralSettingsResponse>> HandleAsync(GetGeneralSettingsRequest request, CancellationToken cancellationToken = default)
    {
        var result = new Result<GetGeneralSettingsResponse>();

        #region # Execute

        var profile = await userSettingsRepo.GetGeneralSettingsAsync(request.UserId, cancellationToken);

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
