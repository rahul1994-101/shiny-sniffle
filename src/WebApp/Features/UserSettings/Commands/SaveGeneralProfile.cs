using FluentValidation;

using WebApp.Features.Shared.Cqrs.Abstractions;

namespace WebApp.Features.UserSettings.Commands;

public sealed record SaveGeneralProfileRequest(Guid UserId, string FirstName, string LastName)
    : ICommand<AppResult<SaveGeneralProfileResponse?>>;

public sealed class SaveGeneralProfileResponse : GeneralSettingsDto
{
}

public sealed class SaveGeneralProfileRequestValidator : AbstractValidator<SaveGeneralProfileRequest>
{
    public SaveGeneralProfileRequestValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User Id is required.");

        RuleFor(x => x.FirstName)
            .NotEmpty()
            .WithMessage("First name is required.")
            .Length(2, 50)
            .WithMessage("First name must be between 2 and 50 characters.");

        RuleFor(x => x.LastName)
            .NotEmpty()
            .WithMessage("Last name is required.")
            .Length(2, 50)
            .WithMessage("Last name must be between 2 and 50 characters.");
    }
}

public sealed class SaveGeneralProfileRequestHandler(UserSettingsRepository userSettingsRepo, SharedRepository sharedRepo)
    : IFeatureHandler<SaveGeneralProfileRequest, AppResult<SaveGeneralProfileResponse?>>
{
    private readonly SharedRepository _sharedRepo = sharedRepo;


    public async Task<AppResult<SaveGeneralProfileResponse?>> HandleAsync(SaveGeneralProfileRequest request, CancellationToken cancellationToken = default)
    {
        var result = new AppResult<SaveGeneralProfileResponse?>();

        #region # Execute

        var profile = await userSettingsRepo.UpdateUserProfileAsync(request.UserId, request.FirstName, request.LastName, request.UserId, cancellationToken);

        #endregion

        #region # Handle Result

        if (profile is null)
        {
            result.Failure(ErrorCode.NotFound, "User not found.");
        }
        else
        {
            result.Success(profile.AsResponse<SaveGeneralProfileResponse>());
        }

        #endregion

        return result;
    }
}
