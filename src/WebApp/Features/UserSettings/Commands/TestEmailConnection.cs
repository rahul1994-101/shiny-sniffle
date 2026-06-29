using FluentValidation;

using WebApp.Features.Shared.Cqrs.Abstractions;
using WebApp.Utilities.Services;

namespace WebApp.Features.UserSettings.Commands;

public sealed record TestEmailConnectionRequest(Guid UserId, EmailSettingsDto Email)
    : ICommand<AppResult<TestEmailConnectionResponse?>>;

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

public sealed class TestEmailConnectionRequestHandler(UserMailboxService mailboxService, SharedRepository sharedRepo)
    : IFeatureHandler<TestEmailConnectionRequest, AppResult<TestEmailConnectionResponse?>>
{
    private readonly SharedRepository _sharedRepo = sharedRepo;


    public async Task<AppResult<TestEmailConnectionResponse?>> HandleAsync(TestEmailConnectionRequest request, CancellationToken cancellationToken = default)
    {
        var result = new AppResult<TestEmailConnectionResponse?>();

        #region # Execute

        var testResult = await mailboxService.TestConnectionAsync(request.UserId, request.Email, cancellationToken);

        #endregion

        #region # Handle Result

        result.Success(new TestEmailConnectionResponse { Result = testResult });

        #endregion

        return result;
    }
}
