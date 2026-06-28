using FluentValidation;

using WebApp.Features._Shared.Abstractions;

namespace WebApp.Features.Settings.Commands;

public sealed record SaveGeneralProfileRequest(Guid UserId, string FirstName, string LastName)
    : ICommand<AppResult<GeneralSettingsResponse?>>;

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

public sealed class SaveGeneralProfileRequestHandler(ISettingsRepository settings)
    : IFeatureHandler<SaveGeneralProfileRequest, AppResult<GeneralSettingsResponse?>>
{
    public async Task<AppResult<GeneralSettingsResponse?>> HandleAsync(
        SaveGeneralProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = new AppResult<GeneralSettingsResponse?>();

        #region # Execute

        var user = await settings.UpdateUserProfileAsync(
            request.UserId,
            request.FirstName,
            request.LastName,
            request.UserId,
            cancellationToken);

        #endregion

        #region # Handle Result

        if (user is null)
        {
            result.Failure(ErrorCode.NotFound, "User not found.");
        }
        else
        {
            result.Success(GeneralSettingsResponse.FromEntity(user));
        }

        #endregion

        return result;
    }
}
