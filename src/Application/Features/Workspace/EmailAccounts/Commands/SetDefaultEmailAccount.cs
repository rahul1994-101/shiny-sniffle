using FluentValidation;

namespace Application.Features.Workspace.EmailAccounts.Commands;

public sealed record SetDefaultEmailAccountRequest(Guid UserId, Guid EmailAccountId)
    : ICommand<SetDefaultEmailAccountResponse>;

public sealed class SetDefaultEmailAccountResponse
{
    public bool Updated { get; init; }
}

public sealed class SetDefaultEmailAccountRequestValidator : AbstractValidator<SetDefaultEmailAccountRequest>
{
    public SetDefaultEmailAccountRequestValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithMessage("User Id is required.");
        RuleFor(x => x.EmailAccountId).NotEmpty().WithMessage("Email account Id is required.");
    }
}

public sealed class SetDefaultEmailAccountRequestHandler(EmailAccountRepository emailAccountRepo)
    : IRequestHandler<SetDefaultEmailAccountRequest, SetDefaultEmailAccountResponse>
{
    public async ValueTask<Result<SetDefaultEmailAccountResponse>> HandleAsync(
        SetDefaultEmailAccountRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = new Result<SetDefaultEmailAccountResponse>();

        #region # Execute

        var (found, error) = await emailAccountRepo.SetDefaultAsync(
            request.UserId,
            request.EmailAccountId,
            request.UserId,
            cancellationToken);

        #endregion

        #region # Handle Result

        if (!found)
        {
            result.Failure(ErrorCode.NotFound, "Email account not found.");
        }
        else if (error is not null)
        {
            result.Failure(ErrorCode.BadRequest, error);
        }
        else
        {
            result.Success(new SetDefaultEmailAccountResponse { Updated = true });
        }

        #endregion

        return result;
    }
}
