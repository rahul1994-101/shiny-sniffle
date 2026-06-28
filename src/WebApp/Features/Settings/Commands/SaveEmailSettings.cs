using FluentValidation;

using WebApp.Features._Shared.Abstractions;

namespace WebApp.Features.Settings.Commands;

public sealed record SaveEmailSettingsRequest(Guid UserId, EmailSettingsResponse Email)
    : ICommand<AppResult<EmailSettingsResponse?>>;

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
    : IFeatureHandler<SaveEmailSettingsRequest, AppResult<EmailSettingsResponse?>>
{
    public async Task<AppResult<EmailSettingsResponse?>> HandleAsync(
        SaveEmailSettingsRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = new AppResult<EmailSettingsResponse?>();

        var existingSettings = await settings.GetUserEmailSettingsAsync(request.UserId, cancellationToken);
        var validationError = EmailSettingsMapping.TryBuildEntity(
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
            request.UserId,
            cancellationToken);

        #endregion

        #region # Handle Result

        result.Success(EmailSettingsMapping.FromEntity(savedSettings));

        #endregion

        return result;
    }
}
