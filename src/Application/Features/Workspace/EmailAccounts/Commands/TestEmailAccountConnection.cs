using Application.Features.dbo.EmailProviders;
using FluentValidation;

namespace Application.Features.workspace.EmailAccounts.Commands;

public sealed record TestEmailAccountConnectionRequest(
    Guid UserId,
    SaveEmailAccountDto Account,
    Guid? AccountId = null)
    : ICommand<TestEmailAccountConnectionResponse>;

public sealed class TestEmailAccountConnectionResponse
{
    public MailboxTestResult Result { get; init; } = null!;
}

public sealed class TestEmailAccountConnectionRequestValidator : AbstractValidator<TestEmailAccountConnectionRequest>
{
    public TestEmailAccountConnectionRequestValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithMessage("User Id is required.");
        RuleFor(x => x.Account).NotNull().WithMessage("Account is required.");
    }
}

public sealed class TestEmailAccountConnectionRequestHandler(
    UserMailboxService mailboxService,
    EmailAccountRepository emailAccountRepo,
    EmailProviderRepository emailProviderRepo)
    : IRequestHandler<TestEmailAccountConnectionRequest, TestEmailAccountConnectionResponse>
{
    public async ValueTask<Result<TestEmailAccountConnectionResponse>> HandleAsync(
        TestEmailAccountConnectionRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = new Result<TestEmailAccountConnectionResponse>();
        var dto = request.Account;

        var (catalog, catalogError) = await EmailSettingsCatalog.LoadCatalogAsync(emailProviderRepo, cancellationToken);
        if (catalogError is not null)
        {
            result.Failure(ErrorCode.BadRequest, catalogError);
            return result;
        }

        var settingsDto = EmailAccountMapping.ToSettingsDto(dto, new EmailSettingsDto { ProviderSlug = dto.ProviderSlug });
        var applyError = EmailSettingsCatalog.TryApplyCatalog(settingsDto, catalog);
        if (applyError is not null)
        {
            result.Failure(ErrorCode.BadRequest, applyError);
            return result;
        }

        var accountId = request.AccountId ?? dto.Id;
        EmailSettings? stored = null;
        if (accountId is { } id)
        {
            stored = await emailAccountRepo.GetEmailSettingsAsync(request.UserId, id, cancellationToken);
        }

        var validationError = EmailSettingsMapping.TryBuildEntity(
            settingsDto,
            stored,
            EmailSettingsBuildMode.Draft,
            out _);

        if (validationError is not null)
        {
            result.Failure(ErrorCode.BadRequest, validationError);
            return result;
        }

        #region # Execute

        var testResult = await mailboxService.TestConnectionAsync(request.UserId, settingsDto, cancellationToken);

        #endregion

        #region # Handle Result

        result.Success(new TestEmailAccountConnectionResponse { Result = testResult });

        #endregion

        return result;
    }
}
