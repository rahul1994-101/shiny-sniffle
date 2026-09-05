using Application.Features.Dbo.EmailProviders;
using FluentValidation;
using Infrastructure.Mailbox;

namespace Application.Features.Workspace.EmailAccounts.Commands;

public sealed record TestEmailAccountConnectionRequest(
    Guid UserId,
    SaveEmailAccountDto Account,
    Guid? EmailAccountId = null)
    : ICommand<TestEmailAccountConnectionResponse>;

public sealed class TestEmailAccountConnectionResponse
{
    public TestConnectionResult Result { get; init; } = null!;
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
    EmailAccountMailboxService emailAccountMailboxService,
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

        #region # Execute

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

        var emailAccountId = request.EmailAccountId ?? dto.Id;
        StoredMailboxSettings? stored = null;
        if (emailAccountId is { } id)
        {
            stored = await emailAccountRepo.GetStoredMailboxSettingsAsync(request.UserId, id, cancellationToken);
        }

        var validationError = EmailSettingsMapping.TryBuildStored(
            settingsDto,
            stored,
            EmailSettingsBuildMode.Draft,
            out _);

        if (validationError is not null)
        {
            result.Failure(ErrorCode.BadRequest, validationError);
            return result;
        }

        var testResult = await emailAccountMailboxService.TestConnectionWithDraftAsync(request.UserId, settingsDto, cancellationToken);

        #endregion

        #region # Handle Result

        result.Success(new TestEmailAccountConnectionResponse { Result = testResult });

        #endregion

        return result;
    }
}
