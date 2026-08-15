using FluentValidation;

namespace Application.Features.Workspace.EmailAccounts.Queries;

public sealed record GetEmailAccountByIdRequest(Guid UserId, Guid EmailAccountId)
    : IQuery<GetEmailAccountByIdResponse>;

public sealed class GetEmailAccountByIdResponse : EmailAccountDto
{
}

public sealed class GetEmailAccountByIdRequestValidator : AbstractValidator<GetEmailAccountByIdRequest>
{
    public GetEmailAccountByIdRequestValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithMessage("User Id is required.");
        RuleFor(x => x.EmailAccountId).NotEmpty().WithMessage("Email account Id is required.");
    }
}

public sealed class GetEmailAccountByIdRequestHandler(EmailAccountRepository emailAccountRepo)
    : IRequestHandler<GetEmailAccountByIdRequest, GetEmailAccountByIdResponse>
{
    public async ValueTask<Result<GetEmailAccountByIdResponse>> HandleAsync(
        GetEmailAccountByIdRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = new Result<GetEmailAccountByIdResponse>();

        #region # Execute

        var account = await emailAccountRepo.GetEmailAccountByIdAsync(request.UserId, request.EmailAccountId, cancellationToken);

        #endregion

        #region # Handle Result

        if (account is null)
        {
            result.Failure(ErrorCode.NotFound, "Email account not found.");
        }
        else
        {
            result.Success(account.AsResponse<GetEmailAccountByIdResponse>());
        }

        #endregion

        return result;
    }
}
