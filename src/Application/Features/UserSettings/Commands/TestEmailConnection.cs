using Application.Features.EmailProviders;
using FluentValidation;

namespace Application.Features.UserSettings.Commands;

public sealed record TestEmailConnectionRequest(Guid UserId, EmailSettingsDto Email)
    : ICommand<TestEmailConnectionResponse>;

public sealed class TestEmailConnectionResponse
{
    public MailboxTestResult Result { get; init; } = null!;
}

public sealed class TestEmailConnectionRequestValidator : AbstractValidator<TestEmailConnectionRequest>
{
    public TestEmailConnectionRequestValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User Id is required.");

        RuleFor(x => x.Email)
            .NotNull()
            .WithMessage("Email settings are required.");
    }
}

public sealed class TestEmailConnectionRequestHandler(
    UserMailboxService mailboxService,
    EmailProviderRepository emailProviderRepo)
    : IRequestHandler<TestEmailConnectionRequest, TestEmailConnectionResponse>
{


    public async ValueTask<Result<TestEmailConnectionResponse>> HandleAsync(TestEmailConnectionRequest request, CancellationToken cancellationToken = default)
    {
        var result = new Result<TestEmailConnectionResponse>();

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

        #region # Execute

        var testResult = await mailboxService.TestConnectionAsync(request.UserId, email, cancellationToken);

        #endregion

        #region # Handle Result

        result.Success(new TestEmailConnectionResponse { Result = testResult });

        #endregion

        return result;
    }
}
