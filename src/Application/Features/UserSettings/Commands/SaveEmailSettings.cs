using Application.Features.EmailAccounts;
using Application.Features.EmailProviders;
using FluentValidation;

namespace Application.Features.UserSettings.Commands;

public sealed record SaveEmailSettingsRequest(Guid UserId, EmailSettingsDto Email)
    : ICommand<SaveEmailSettingsResponse>;

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

public sealed class SaveEmailSettingsRequestHandler(
    EmailAccountRepository emailAccountRepo,
    EmailProviderRepository emailProviderRepo)
    : IRequestHandler<SaveEmailSettingsRequest, SaveEmailSettingsResponse>
{
    public async ValueTask<Result<SaveEmailSettingsResponse>> HandleAsync(SaveEmailSettingsRequest request, CancellationToken cancellationToken = default)
    {
        var result = new Result<SaveEmailSettingsResponse>();

        var (catalog, catalogError) = await EmailSettingsCatalog.LoadCatalogAsync(emailProviderRepo, cancellationToken);
        if (catalogError is not null)
        {
            result.Failure(ErrorCode.BadRequest, catalogError);
            return result;
        }

        var email = request.Email;
        var applyError = EmailSettingsCatalog.TryApplyCatalog(email, catalog);
        if (applyError is not null)
        {
            result.Failure(ErrorCode.BadRequest, applyError);
            return result;
        }

        var existingSettings = await emailAccountRepo.GetDefaultEmailSettingsAsync(request.UserId, cancellationToken);
        var validationError = EmailSettingsMapping.TryBuildEntity(email, existingSettings, EmailSettingsBuildMode.Save, out var newSettings);

        if (validationError is not null)
        {
            result.Failure(ErrorCode.BadRequest, validationError);
            return result;
        }

        #region # Execute

        var savedSettings = await emailAccountRepo.SaveDefaultEmailSettingsAsync(
            request.UserId,
            newSettings,
            request.UserId,
            cancellationToken);

        #endregion

        #region # Handle Result

        result.Success(EmailSettingsMapping.FromEntity(savedSettings).AsResponse<SaveEmailSettingsResponse>());

        #endregion

        return result;
    }
}
