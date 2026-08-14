using FluentValidation;

namespace Application.Features.Workspace.EmailAccounts.Queries;

public sealed record GetAllEmailAccountsByUserIdRequest(Guid UserId)
    : IQuery<GetAllEmailAccountsByUserIdResponse>;

public sealed class GetAllEmailAccountsByUserIdResponse
{
    public IReadOnlyList<EmailAccountSummaryDto> Accounts { get; init; } = [];
}

public sealed class GetAllEmailAccountsByUserIdRequestValidator : AbstractValidator<GetAllEmailAccountsByUserIdRequest>
{
    public GetAllEmailAccountsByUserIdRequestValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithMessage("User Id is required.");
    }
}

public sealed class GetAllEmailAccountsByUserIdRequestHandler(EmailAccountRepository emailAccountRepo)
    : IRequestHandler<GetAllEmailAccountsByUserIdRequest, GetAllEmailAccountsByUserIdResponse>
{
    public async ValueTask<Result<GetAllEmailAccountsByUserIdResponse>> HandleAsync(
        GetAllEmailAccountsByUserIdRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = new Result<GetAllEmailAccountsByUserIdResponse>();

        #region # Execute

        var accounts = await emailAccountRepo.GetAllEmailAccountsByUserIdAsync(request.UserId, cancellationToken);

        #endregion

        #region # Handle Result

        result.Success(new GetAllEmailAccountsByUserIdResponse { Accounts = accounts });

        #endregion

        return result;
    }
}
