using FluentValidation;

using WebApp.Features._Shared.Abstractions;
using WebApp.Utilities.Services;

namespace WebApp.Features.Settings.Commands;

public sealed record TestEmailConnectionCommand(Guid UserId, EmailSettingsDto Email)
    : ICommand<AppResult<MailboxTestResult?>>;

public sealed class TestEmailConnectionCommandValidator : AbstractValidator<TestEmailConnectionCommand>
{
    public TestEmailConnectionCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User Id is required.");

        RuleFor(x => x.Email)
            .NotNull()
            .WithMessage("Email settings are required.");
    }
}

public sealed class TestEmailConnectionCommandHandler(UserMailboxService mailboxService)
    : IFeatureHandler<TestEmailConnectionCommand, AppResult<MailboxTestResult?>>
{
    public async Task<AppResult<MailboxTestResult?>> HandleAsync(
        TestEmailConnectionCommand command,
        CancellationToken cancellationToken = default)
    {
        var result = new AppResult<MailboxTestResult?>();

        #region # Execute

        var testResult = await mailboxService.TestConnectionAsync(command.UserId, command.Email);

        #endregion

        #region # Handle Result

        result.Success(testResult);

        #endregion

        return result;
    }
}
