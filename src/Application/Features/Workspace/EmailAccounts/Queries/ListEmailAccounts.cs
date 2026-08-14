using FluentValidation;

namespace Application.Features.Workspace.EmailAccounts.Queries;

public sealed record ListEmailAccountsRequest(Guid UserId)
    : IQuery<ListEmailAccountsResponse>;

public sealed class ListEmailAccountsResponse
{
    public IReadOnlyList<EmailAccountSummaryDto> Accounts { get; init; } = [];
}

public sealed class ListEmailAccountsRequestValidator : AbstractValidator<ListEmailAccountsRequest>
{
    public ListEmailAccountsRequestValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithMessage("User Id is required.");
    }
}

public sealed class ListEmailAccountsRequestHandler(EmailAccountRepository emailAccountRepo)
    : IRequestHandler<ListEmailAccountsRequest, ListEmailAccountsResponse>
{
    public async ValueTask<Result<ListEmailAccountsResponse>> HandleAsync(
        ListEmailAccountsRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = new Result<ListEmailAccountsResponse>();

        #region # Execute

        var accounts = await emailAccountRepo.ListAsync(request.UserId, cancellationToken);

        #endregion

        #region # Handle Result

        result.Success(new ListEmailAccountsResponse { Accounts = accounts });

        #endregion

        return result;
    }
}
