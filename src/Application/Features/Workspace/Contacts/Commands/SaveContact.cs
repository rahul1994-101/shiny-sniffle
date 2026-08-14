namespace Application.Features.Workspace.Contacts.Commands;

using Application.Features.Workspace.Contacts;
using FluentValidation;

public sealed record SaveContactRequest(Guid UserId, SaveContactDto Contact) : ICommand<SaveContactResponse>;

public sealed class SaveContactResponse : ContactDto
{
}

public sealed class SaveContactRequestValidator : AbstractValidator<SaveContactRequest>
{
    public SaveContactRequestValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithMessage("User Id is required.");
        RuleFor(x => x.Contact).NotNull().WithMessage("Contact is required.");
    }
}

public sealed class SaveContactRequestHandler(ContactRepository contactRepo)
    : IRequestHandler<SaveContactRequest, SaveContactResponse>
{
    public async ValueTask<Result<SaveContactResponse>> HandleAsync(
        SaveContactRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = new Result<SaveContactResponse>();

        #region # Execute

        var validation = ContactMapping.ValidateSave(request.Contact);
        if (validation is not null)
        {
            result.Failure(ErrorCode.BadRequest, validation);
            return result;
        }

        var (saved, error, notFound) = await contactRepo.SaveAsync(
            request.UserId,
            request.Contact,
            request.UserId,
            cancellationToken);

        #endregion

        #region # Handle Result

        if (notFound)
        {
            result.Failure(ErrorCode.NotFound, "Contact not found.");
        }
        else if (error is not null)
        {
            result.Failure(ErrorCode.BadRequest, error);
        }
        else if (saved is null)
        {
            result.Failure(ErrorCode.InternalServerError, "Failed to save contact.");
        }
        else
        {
            result.Success(saved.AsResponse<SaveContactResponse>());
        }

        #endregion

        return result;
    }
}
