using Application.Features.Dbo.EmailProviders;
using FluentValidation;

namespace Application.Features.Workspace.EmailAccounts.Commands;

public sealed record SaveEmailAccountRequest(Guid UserId, SaveEmailAccountDto Account)
    : ICommand<SaveEmailAccountResponse>;

public sealed class SaveEmailAccountResponse : EmailAccountDto
{
}

public sealed class SaveEmailAccountRequestValidator : AbstractValidator<SaveEmailAccountRequest>
{
    public SaveEmailAccountRequestValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithMessage("User Id is required.");
        RuleFor(x => x.Account).NotNull().WithMessage("Account is required.");
    }
}

public sealed class SaveEmailAccountRequestHandler(
    EmailAccountRepository emailAccountRepo,
    EmailProviderRepository emailProviderRepo)
    : IRequestHandler<SaveEmailAccountRequest, SaveEmailAccountResponse>
{
    public async ValueTask<Result<SaveEmailAccountResponse>> HandleAsync(
        SaveEmailAccountRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = new Result<SaveEmailAccountResponse>();
        var dto = request.Account;

        #region # Execute

        var fieldError = EmailAccountMapping.ValidateSave(dto);
        if (fieldError is not null)
        {
            result.Failure(ErrorCode.BadRequest, fieldError);
            return result;
        }

        var (catalog, catalogError) = await EmailSettingsCatalog.LoadCatalogAsync(emailProviderRepo, request.UserId, cancellationToken);
        if (catalogError is not null)
        {
            result.Failure(ErrorCode.BadRequest, catalogError);
            return result;
        }

        var settingsDto = EmailAccountMapping.ToSettingsDto(dto);
        var applyError = EmailSettingsCatalog.TryApplyCatalog(settingsDto, catalog);
        if (applyError is not null)
        {
            result.Failure(ErrorCode.BadRequest, applyError);
            return result;
        }

        StoredMailboxSettings? existing = null;
        if (dto.Id is { } id)
        {
            existing = await emailAccountRepo.GetStoredMailboxSettingsAsync(request.UserId, id, cancellationToken);
        }

        var validationError = EmailSettingsMapping.TryBuildStored(
            settingsDto,
            existing,
            EmailSettingsBuildMode.Save,
            out var builtSettings);

        if (validationError is not null)
        {
            result.Failure(ErrorCode.BadRequest, validationError);
            return result;
        }

        var (saved, saveError, notFound) = await emailAccountRepo.SaveAsync(
            request.UserId,
            dto,
            builtSettings!,
            request.UserId,
            cancellationToken);

        #endregion

        #region # Handle Result

        if (notFound)
        {
            result.Failure(ErrorCode.NotFound, "Email account not found.");
        }
        else if (saveError is not null)
        {
            result.Failure(ErrorCode.BadRequest, saveError);
        }
        else if (saved is null)
        {
            result.Failure(ErrorCode.InternalServerError, "Failed to save email account.");
        }
        else
        {
            result.Success(saved.AsResponse<SaveEmailAccountResponse>());
        }

        #endregion

        return result;
    }
}
