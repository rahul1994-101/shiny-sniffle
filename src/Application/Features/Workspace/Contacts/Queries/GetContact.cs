namespace Application.Features.workspace.Contacts.Queries;

using Application.Features.workspace.Contacts;
using FluentValidation;

public sealed record GetContactRequest(Guid UserId, Guid ContactId) : IQuery<GetContactResponse>;

public sealed class GetContactResponse : ContactDto
{
}

public sealed class GetContactRequestValidator : AbstractValidator<GetContactRequest>
{
    public GetContactRequestValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithMessage("User Id is required.");
        RuleFor(x => x.ContactId).NotEmpty().WithMessage("Contact Id is required.");
    }
}

public sealed class GetContactRequestHandler(ContactRepository contactRepo)
    : IRequestHandler<GetContactRequest, GetContactResponse>
{
    public async ValueTask<Result<GetContactResponse>> HandleAsync(
        GetContactRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = new Result<GetContactResponse>();

        #region # Execute

        var contact = await contactRepo.GetByIdAsync(request.UserId, request.ContactId, cancellationToken);

        #endregion

        #region # Handle Result

        if (contact is null)
        {
            result.Failure(ErrorCode.NotFound, "Contact not found.");
        }
        else
        {
            result.Success(contact.AsResponse<GetContactResponse>());
        }

        #endregion

        return result;
    }
}
