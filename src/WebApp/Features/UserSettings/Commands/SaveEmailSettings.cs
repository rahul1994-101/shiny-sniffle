using FluentValidation;

using WebApp.Features.Shared.Cqrs.Abstractions;

namespace WebApp.Features.UserSettings.Commands;

public sealed record SaveEmailSettingsRequest(Guid UserId, EmailSettingsDto Email)
    : ICommand<AppResult<SaveEmailSettingsResponse?>>;

public sealed class SaveEmailSettingsResponse : EmailSettingsDto
{
}

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

public sealed class SaveEmailSettingsRequestHandler(UserSettingsRepository userSettingsRepo, SharedRepository sharedRepo)
    : IFeatureHandler<SaveEmailSettingsRequest, AppResult<SaveEmailSettingsResponse?>>
{
    private readonly SharedRepository _sharedRepo = sharedRepo;


    public async Task<AppResult<SaveEmailSettingsResponse?>> HandleAsync(SaveEmailSettingsRequest request, CancellationToken cancellationToken = default)
    {
        var result = new AppResult<SaveEmailSettingsResponse?>();

        var existingSettings = await userSettingsRepo.GetUserEmailSettingsAsync(request.UserId, cancellationToken);
        var validationError = EmailSettingsMapping.TryBuildEntity(request.Email, existingSettings, EmailSettingsBuildMode.Save, out var newSettings);

        if (validationError is not null)
        {
            result.Failure(ErrorCode.BadRequest, validationError);
            return result;
        }

        #region # Execute

        var savedSettings = await userSettingsRepo.SaveUserEmailSettingsAsync(request.UserId, newSettings, request.UserId, cancellationToken);

        #endregion

        #region # Handle Result

        result.Success(EmailSettingsMapping.FromEntity(savedSettings).AsResponse<SaveEmailSettingsResponse>());

        #endregion

        return result;
    }
}
