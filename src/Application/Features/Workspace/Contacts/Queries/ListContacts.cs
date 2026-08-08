namespace Application.Features.Workspace.Contacts.Queries;

using Application.Features.Workspace.Contacts;
using FluentValidation;

public sealed record ListContactsRequest(Guid UserId) : IQuery<ListContactsResponse>;

public sealed class ListContactsResponse
{
    public IReadOnlyList<ContactSummaryDto> Contacts { get; init; } = [];
}

public sealed class ListContactsRequestValidator : AbstractValidator<ListContactsRequest>
{
    public ListContactsRequestValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithMessage("User Id is required.");
    }
}

public sealed class ListContactsRequestHandler(ContactRepository contactRepo)
    : IRequestHandler<ListContactsRequest, ListContactsResponse>
{
    public async ValueTask<Result<ListContactsResponse>> HandleAsync(
        ListContactsRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = new Result<ListContactsResponse>();
        var contacts = await contactRepo.ListAsync(request.UserId, cancellationToken);
        result.Success(new ListContactsResponse { Contacts = contacts });
        return result;
    }
}
