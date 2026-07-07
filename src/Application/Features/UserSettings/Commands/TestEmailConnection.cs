using FluentValidation;

using Application.Services;

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

public sealed class TestEmailConnectionRequestHandler(UserMailboxService mailboxService, SharedRepository sharedRepo)
    : IRequestHandler<TestEmailConnectionRequest, TestEmailConnectionResponse>
{
    private readonly SharedRepository _sharedRepo = sharedRepo;


    public async ValueTask<Result<TestEmailConnectionResponse>> HandleAsync(TestEmailConnectionRequest request, CancellationToken cancellationToken = default)
    {
        var result = new Result<TestEmailConnectionResponse>();

        #region # Execute

        var testResult = await mailboxService.TestConnectionAsync(request.UserId, request.Email, cancellationToken);

        #endregion

        #region # Handle Result

        result.Success(new TestEmailConnectionResponse { Result = testResult });

        #endregion

        return result;
    }
}
