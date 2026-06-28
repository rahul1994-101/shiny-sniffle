using FluentValidation;

using WebApp.Features._Shared.Abstractions;

namespace WebApp.Features.Settings.Commands;

public sealed record SaveGeneralProfileCommand(SaveGeneralProfileRequest Request)
    : ICommand<AppResult<GeneralSettingsDto?>>;

public sealed class SaveGeneralProfileCommandValidator : AbstractValidator<SaveGeneralProfileCommand>
{
    public SaveGeneralProfileCommandValidator()
    {
        RuleFor(x => x.Request)
            .NotNull()
            .WithMessage("Request can't be empty.");

        RuleFor(x => x.Request.UserId)
            .NotEmpty()
            .WithMessage("User Id is required.");

        RuleFor(x => x.Request.FirstName)
            .NotEmpty()
            .WithMessage("First name is required.")
            .Length(2, 50)
            .WithMessage("First name must be between 2 and 50 characters.");

        RuleFor(x => x.Request.LastName)
            .NotEmpty()
            .WithMessage("Last name is required.")
            .Length(2, 50)
            .WithMessage("Last name must be between 2 and 50 characters.");
    }
}

public sealed class SaveGeneralProfileCommandHandler(ISettingsRepository settings)
    : IFeatureHandler<SaveGeneralProfileCommand, AppResult<GeneralSettingsDto?>>
{
    public async Task<AppResult<GeneralSettingsDto?>> HandleAsync(
        SaveGeneralProfileCommand command,
        CancellationToken cancellationToken = default)
    {
        var result = new AppResult<GeneralSettingsDto?>();
        var request = command.Request;

        #region # Execute

        var savedProfile = await settings.UpdateUserProfileAsync(
            request.UserId,
            request.FirstName,
            request.LastName,
            request.UserId);

        #endregion

        #region # Handle Result

        if (savedProfile is null)
        {
            result.Failure(ErrorCode.NotFound, "User not found.");
        }
        else
        {
            result.Success(savedProfile);
        }

        #endregion

        return result;
    }
}
