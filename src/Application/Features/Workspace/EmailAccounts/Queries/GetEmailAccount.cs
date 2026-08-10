using FluentValidation;

namespace Application.Features.workspace.EmailAccounts.Queries;

public sealed record GetEmailAccountRequest(Guid UserId, Guid AccountId)
    : IQuery<GetEmailAccountResponse>;

public sealed class GetEmailAccountResponse : EmailAccountDto
{
}

public sealed class GetEmailAccountRequestValidator : AbstractValidator<GetEmailAccountRequest>
{
    public GetEmailAccountRequestValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithMessage("User Id is required.");
        RuleFor(x => x.AccountId).NotEmpty().WithMessage("Account Id is required.");
    }
}

public sealed class GetEmailAccountRequestHandler(EmailAccountRepository emailAccountRepo)
    : IRequestHandler<GetEmailAccountRequest, GetEmailAccountResponse>
{
    public async ValueTask<Result<GetEmailAccountResponse>> HandleAsync(
        GetEmailAccountRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = new Result<GetEmailAccountResponse>();

        #region # Execute

        var account = await emailAccountRepo.GetByIdAsync(request.UserId, request.AccountId, cancellationToken);

        #endregion

        #region # Handle Result

        if (account is null)
        {
            result.Failure(ErrorCode.NotFound, "Email account not found.");
        }
        else
        {
            result.Success(account.AsResponse<GetEmailAccountResponse>());
        }

        #endregion

        return result;
    }
}
