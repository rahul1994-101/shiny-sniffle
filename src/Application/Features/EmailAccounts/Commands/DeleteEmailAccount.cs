using FluentValidation;

namespace Application.Features.EmailAccounts.Commands;

public sealed record DeleteEmailAccountRequest(Guid UserId, Guid AccountId)
    : ICommand<DeleteEmailAccountResponse>;

public sealed class DeleteEmailAccountResponse
{
    public bool Deleted { get; init; }
}

public sealed class DeleteEmailAccountRequestValidator : AbstractValidator<DeleteEmailAccountRequest>
{
    public DeleteEmailAccountRequestValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithMessage("User Id is required.");
        RuleFor(x => x.AccountId).NotEmpty().WithMessage("Account Id is required.");
    }
}

public sealed class DeleteEmailAccountRequestHandler(EmailAccountRepository emailAccountRepo)
    : IRequestHandler<DeleteEmailAccountRequest, DeleteEmailAccountResponse>
{
    public async ValueTask<Result<DeleteEmailAccountResponse>> HandleAsync(
        DeleteEmailAccountRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = new Result<DeleteEmailAccountResponse>();

        #region # Execute

        var (found, error) = await emailAccountRepo.SoftDeleteAsync(
            request.UserId,
            request.AccountId,
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
            result.Success(new DeleteEmailAccountResponse { Deleted = true });
        }

        #endregion

        return result;
    }
}
