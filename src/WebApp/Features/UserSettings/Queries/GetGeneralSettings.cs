using FluentValidation;

using WebApp.Features._Shared.Abstractions;

namespace WebApp.Features.UserSettings.Queries;

public sealed record GetGeneralSettingsRequest(Guid UserId)
    : IQuery<AppResult<GetGeneralSettingsResponse?>>;

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
    : IFeatureHandler<GetGeneralSettingsRequest, AppResult<GetGeneralSettingsResponse?>>
{
    private readonly SharedRepository _sharedRepo = sharedRepo;


    public async Task<AppResult<GetGeneralSettingsResponse?>> HandleAsync(GetGeneralSettingsRequest request, CancellationToken cancellationToken = default)
    {
        var result = new AppResult<GetGeneralSettingsResponse?>();

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
