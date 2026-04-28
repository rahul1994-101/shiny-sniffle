using AAF.Utilities;

using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;

namespace AAF.Models;

#region # Common

public class AppResult
{
    #region # Init

    public AppResult()
    {
        HasError = false;
        Errors = new Collection<AppError>();
    }

    #endregion

    public bool HasError { get; protected set; }

    public Collection<AppError> Errors { get; }

    public virtual bool Validate<T>(T model) where T : class
    {
        if (model is null)
        {
            Failure(ErrorCode.BadRequest, $"{nameof(model)} can't be empty.");
        }
        else
        {
            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(model, null, null);

            var isValid = Validator.TryValidateObject(model, validationContext, validationResults, true);
            if (isValid)
            {
                Success();
            }
            else
            {
                foreach (var validationResult in validationResults)
                {
                    Failure(ErrorCode.BadRequest, validationResult.ErrorMessage ?? "Unknown Error!!");
                }
            }
        }
        return HasError;
    }

    public virtual void Success()
    {
        HasError = false;
        Errors.Clear();
    }

    public virtual void Failure(ErrorCode code, string message)
    {
        HasError = true;
        Errors.Add(new AppError(code, message));
    }
}

public class AppResult<T> : AppResult
{
    #region # Init

    public AppResult() : base() { }

    #endregion

    public T? Payload { get; protected set; }

    public void Success(T? payload)
    {
        base.Success();
        Payload = payload;
    }
}

public class AppError
{
    #region # Init

    public AppError() { }

    public AppError(ErrorCode code, string message)
    {
        Code = code;
        Message = message;
    }

    #endregion

    public ErrorCode Code { get; set; }
    public string Message { get; set; } = string.Empty;
}

#endregion

#region # AppSettings.json

public class OrgSettings
{
    public string Name { get; set; } = string.Empty;
    public string ContactInfo { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string SiteUrl { get; set; } = string.Empty;

}

public class SmtpSettings
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
}

#endregion

public class SignInRequest
{
    [Required(ErrorMessage = "Email Id is required.")]
    [StringLength(100, MinimumLength = 5, ErrorMessage = "Email Id must be between 5 and 100 characters.")]
    [ValidEmailAddress]
    [NoLeadingOrTrailingSpaces]
    public string EmailId { get; set; }

    [Required(ErrorMessage = "Password is required.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be between 6 and 100 characters.")]
    [NoSpaces]
    public string Password { get; set; }
}

public class ChangePasswordDTO
{
    [Required(ErrorMessage = "UserId is required")]
    public long UserId { get; set; }

    [Required(ErrorMessage = "Old Password is required")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be between 6 and 100 characters.")]
    [NoSpaces]
    public string OldPassword { get; set; }

    [Required(ErrorMessage = "New Password is required")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "New Password must be between 6 and 100 characters.")]
    [NoSpaces]
    public string NewPassword { get; set; }

    [Required(ErrorMessage = "Confirm Password is required")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Confirm Password must be between 6 and 100 characters.")]
    [NoSpaces]
    public string ConfirmPassword { get; set; }
}

public class ForgetPasswordDTO
{
    [Required(ErrorMessage = "Email Id is required.")]
    [StringLength(100, MinimumLength = 5, ErrorMessage = "Email Id must be between 5 and 100 characters.")]
    [ValidEmailAddress]
    [NoLeadingOrTrailingSpaces]
    public string EmailId { get; set; }
}


public class PaginatedList<T>
{
    public int PageSize { get; set; }

    public int CurrentPage { get; set; }

    public int TotalPages { get; set; }

    public int TotalRecords { get; set; }

    public IEnumerable<T> Records { get; set; }
}

public class DropdownItem
{
    public long Key { get; set; }
    public string Value { get; set; }
}

public class StateDropdownItem
{
    public string Key { get; set; }
    public string Value { get; set; }
}


public class FamilyListForAdminDTO
{
    public long Id { get; set; }
    public string ClientCode { get; set; }
    public string ClientName { get; set; }
    public string Program { get; set; }
    public string SalesForceNumber { get; set; }
    public bool NeedMealKit { get; set; }
    public int NumberOfChildrens { get; set; }
    public string RegisteredBy { get; set; }
    public Status Status { get; set; }

    public string DonorCompanyName { get; set; }
    public string DonorFirstName { get; set; }
    public string DonorLastName { get; set; }
    public string DonorEmailId { get; set; }
    public string DonorMobileNo { get; set; }
}

public class FamilyListForAdvocateDTO
{
    public long Id { get; set; }

    public string ClientCode { get; set; }
    public string ClientName { get; set; }
    public string Program { get; set; }
    public string SalesForceNumber { get; set; }
    public bool NeedMealKit { get; set; }
    public int NumberOfChildrens { get; set; }
    public string RegisteredBy { get; set; }
    public Status Status { get; set; }
}

public class CampaignDataForDonorDTO
{
    public long Id { get; set; }
    public string Name { get; set; }
    public string Instructions { get; set; }

    public List<FamilyWithChildrenForDonorDTO>? Families { get; set; }
}

public class FamilyWithChildrenForDonorDTO
{
    public long Id { get; set; }
    public long MemberCount { get; set; }
    public string MemberCountString => ConvertNumberToWord.GetMemberCountString(MemberCount);

    public bool IsSelected { get; set; }
}


public class DashboardDetailsDTO
{
    // All Stats
    public int TotalRegistered { get; set; }
    public int TotalAdopted { get; set; }
    public int TotalReceived { get; set; }
    public int TotalDistributed { get; set; }

    // For Advocate
    public int TotalRegisteredByAdvocate { get; set; }
    public int TotalAdoptedByAdvocate { get; set; }
    public int TotalReceivedByAdvocate { get; set; }
    public int TotalDistributedByAdvocate { get; set; }
}

