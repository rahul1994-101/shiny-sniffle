namespace Application.Features.Workspace.Contacts.Queries;

using Application.Features.Workspace.Contacts;
using FluentValidation;

public sealed record GetContactByIdRequest(Guid UserId, Guid ContactId) : IQuery<GetContactByIdResponse>;

public sealed class GetContactByIdResponse : ContactDto
{
}

public sealed class GetContactByIdRequestValidator : AbstractValidator<GetContactByIdRequest>
{
    public GetContactByIdRequestValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithMessage("User Id is required.");
        RuleFor(x => x.ContactId).NotEmpty().WithMessage("Contact Id is required.");
    }
}

public sealed class GetContactByIdRequestHandler(ContactRepository contactRepo)
    : IRequestHandler<GetContactByIdRequest, GetContactByIdResponse>
{
    public async ValueTask<Result<GetContactByIdResponse>> HandleAsync(
        GetContactByIdRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = new Result<GetContactByIdResponse>();

        #region # Execute

        var contact = await contactRepo.GetContactByIdAsync(request.UserId, request.ContactId, cancellationToken);

        #endregion

        #region # Handle Result

        if (contact is null)
        {
            result.Failure(ErrorCode.NotFound, "Contact not found.");
        }
        else
        {
            result.Success(contact.AsResponse<GetContactByIdResponse>());
        }

        #endregion

        return result;
    }
}
