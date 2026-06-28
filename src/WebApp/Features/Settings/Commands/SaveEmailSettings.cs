using FluentValidation;

using WebApp.Features._Shared.Abstractions;
using WebApp.Utilities.Helpers;

namespace WebApp.Features.Settings.Commands;

public sealed record SaveEmailSettingsRequest(Guid UserId, EmailSettingsDto Email)
    : ICommand<AppResult<SaveEmailSettingsResponse?>>;

public sealed class SaveEmailSettingsRequestValidator : AbstractValidator<SaveEmailSettingsRequest>
{
    public SaveEmailSettingsRequestValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User Id is required.");

        RuleFor(x => x.Email)
            .NotNull()
            .WithMessage("Email settings are required.");
    }
}

public sealed class SaveEmailSettingsRequestHandler(ISettingsRepository settings)
    : IFeatureHandler<SaveEmailSettingsRequest, AppResult<SaveEmailSettingsResponse?>>
{
    public async Task<AppResult<SaveEmailSettingsResponse?>> HandleAsync(
        SaveEmailSettingsRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = new AppResult<SaveEmailSettingsResponse?>();

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

        var savedDto = EmailSettingsHelpers.ToDto(savedSettings);
        if (savedDto is null)
        {
            result.Failure(ErrorCode.InternalServerError, "Failed to save email settings.");
        }
        else
        {
            result.Success(new SaveEmailSettingsResponse { Email = savedDto });
        }

        #endregion

        return result;
    }
}

public sealed class SaveEmailSettingsResponse
{
    public EmailSettingsDto Email { get; init; } = null!;
}
