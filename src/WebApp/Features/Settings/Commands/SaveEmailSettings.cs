using FluentValidation;

using WebApp.Features._Shared.Abstractions;
using WebApp.Utilities.Helpers;

namespace WebApp.Features.Settings.Commands;

public sealed record SaveEmailSettingsCommand(SaveEmailSettingsRequest Request)
    : ICommand<AppResult<EmailSettingsDto?>>;

public sealed class SaveEmailSettingsCommandValidator : AbstractValidator<SaveEmailSettingsCommand>
{
    public SaveEmailSettingsCommandValidator()
    {
        RuleFor(x => x.Request)
            .NotNull()
            .WithMessage("Request can't be empty.");

        RuleFor(x => x.Request.UserId)
            .NotEmpty()
            .WithMessage("User Id is required.");

        RuleFor(x => x.Request.Email)
            .NotNull()
            .WithMessage("Email settings are required.");
    }
}

public sealed class SaveEmailSettingsCommandHandler(ISettingsRepository settings)
    : IFeatureHandler<SaveEmailSettingsCommand, AppResult<EmailSettingsDto?>>
{
    public async Task<AppResult<EmailSettingsDto?>> HandleAsync(
        SaveEmailSettingsCommand command,
        CancellationToken cancellationToken = default)
    {
        var result = new AppResult<EmailSettingsDto?>();
        var request = command.Request;

        var existingSettings = await settings.GetUserEmailSettingsAsync(request.UserId);
        var validationError = EmailSettingsHelpers.TryBuildFromDto(
            request.Email,
            existingSettings,
            EmailSettingsBuildMode.Save,
            out var newSettings);

        if (validationError is not null)
        {
            result.Failure(ErrorCode.BadRequest, validationError);
            return result;
        }

        #region # Execute

        var savedSettings = await settings.SaveUserEmailSettingsAsync(
            request.UserId,
            newSettings,
            request.UserId);

        #endregion

        #region # Handle Result

        result.Success(EmailSettingsHelpers.ToDto(savedSettings));

        #endregion

        return result;
    }
}
