namespace Application.Features.Workspace.Contacts.Queries;

using Application.Features.Workspace.Contacts;
using FluentValidation;

public sealed record GetAllContactsByUserIdRequest(Guid UserId) : IQuery<GetAllContactsByUserIdResponse>;

public sealed class GetAllContactsByUserIdResponse
{
    public IReadOnlyList<ContactSummaryDto> Contacts { get; init; } = [];
}

public sealed class GetAllContactsByUserIdRequestValidator : AbstractValidator<GetAllContactsByUserIdRequest>
{
    public GetAllContactsByUserIdRequestValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithMessage("User Id is required.");
    }
}

public sealed class GetAllContactsByUserIdRequestHandler(ContactRepository contactRepo)
    : IRequestHandler<GetAllContactsByUserIdRequest, GetAllContactsByUserIdResponse>
{
    public async ValueTask<Result<GetAllContactsByUserIdResponse>> HandleAsync(
        GetAllContactsByUserIdRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = new Result<GetAllContactsByUserIdResponse>();

        #region # Execute

        var contacts = await contactRepo.GetAllContactsByUserIdAsync(request.UserId, cancellationToken);

        #endregion

        #region # Handle Result

        result.Success(new GetAllContactsByUserIdResponse { Contacts = contacts });

        #endregion

        return result;
    }
}
