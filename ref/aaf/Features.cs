using AAF.Models;
using AAF.Utilities;

namespace AAF.Data;

public sealed class Features
{
    #region # Init

    public Features(AppDbContext ctx, Persistence repo, Mailer mailer, AuthService authService)
    {
        _ctx = ctx;
        _repo = repo;
        _mailer = mailer;
        _authService = authService;
    }

    private readonly AppDbContext _ctx;
    private readonly Persistence _repo;
    private readonly Mailer _mailer;
    private readonly AuthService _authService;

    #endregion

    public async Task<AppResult<User?>> SignInAsync(SignInRequest signInRequest)
    {
        var result = new AppResult<User?>();
        try
        {
            #region # Validate

            var hasError = result.Validate(signInRequest);
            if (hasError)
            {
                return result;
            }

            #endregion

            #region # Execute

            var user = await _repo.SignInAsync(signInRequest);

            #endregion

            #region # Handle Result

            if (user is null)
            {
                result.Failure(ErrorCode.NotFound, "Invalid Credentials");
            }
            else
            {
                result.Success(user);
            }

            #endregion
        }
        catch (Exception ex)
        {
            result.Failure(ErrorCode.InternalServerError, ex.Message);
        }
        return result;
    }

    public async Task<AppResult> ChangePasswordForAdvocateAsync(ChangePasswordDTO changePasswordDTO)
    {
        var result = new AppResult();
        try
        {
            #region # Validate

            var hasError = result.Validate(changePasswordDTO);
            if (hasError)
            {
                return result;
            }

            var confirmPasswordMatch = changePasswordDTO.NewPassword == changePasswordDTO.ConfirmPassword;
            if (!confirmPasswordMatch)
            {
                result.Failure(ErrorCode.BadRequest, "The new password and confirm password do not match.");
                return result;
            }

            var user = await _repo.GetUserByIdAsync(changePasswordDTO.UserId);
            if (user == null)
            {
                result.Failure(ErrorCode.BadRequest, "User not found.");
                return result;
            }

            var isCurrentPasswordValid = user.Password == changePasswordDTO.OldPassword;
            if (!isCurrentPasswordValid)
            {
                result.Failure(ErrorCode.BadRequest, "The old password provided is incorrect.");
                return result;
            }

            var existingPassword = user.Password == changePasswordDTO.NewPassword;
            if (existingPassword)
            {
                result.Failure(ErrorCode.BadRequest, "The new password must be different from the old password.");
                return result;
            }

            #endregion

            #region # Execute

            var isPasswordUpdated = await _repo.ChangePasswordAsync(changePasswordDTO.UserId, changePasswordDTO.NewPassword);

            if (!isPasswordUpdated)
            {
                result.Failure(ErrorCode.InternalServerError, "Something went wrong.");
            }
            else
            {
                result.Success();
            }

            #endregion
        }
        catch (Exception ex)
        {
            result.Failure(ErrorCode.InternalServerError, ex.Message);
        }
        return result;
    }

    public async Task<AppResult<ForgetPasswordDTO?>> ForgetPasswordAsync(ForgetPasswordDTO forgetPassword)
    {
        var result = new AppResult<ForgetPasswordDTO?>();
        try
        {
            #region # Validate

            var hasError = result.Validate(forgetPassword);
            if (hasError)
            {
                return result;
            }

            var user = await _repo.FindUserByEmailIdAsync(forgetPassword.EmailId);
            if (user is null)
            {
                result.Failure(ErrorCode.BadRequest, "No user account found for this emailId.");
                return result;
            }

            #endregion

            #region # Execute

            await _mailer.SendForgetPasswordMailAsync(user);

            result.Success();

            #endregion
        }
        catch (Exception ex)
        {
            result.Failure(ErrorCode.InternalServerError, ex.Message);
        }
        return result;
    }

    #region # Campaign

    public async Task<AppResult<PaginatedList<Campaign>>> GetAllCampaignsAsync(int currentPage, int pageSize, string? searchTerm = null)
    {
        var result = new AppResult<PaginatedList<Campaign>>();
        try
        {
            #region # Validate

            if (currentPage <= 0 || pageSize <= 0)
            {
                result.Failure(ErrorCode.BadRequest, "Invalid pagination parameters.");
                return result;
            }

            #endregion

            #region # Execute

            var paginatedCampaigns = await _repo.GetAllCampaignsAsync(currentPage, pageSize, searchTerm);

            result.Success(paginatedCampaigns);

            #endregion
        }
        catch (Exception ex)
        {
            result.Failure(ErrorCode.InternalServerError, ex.Message);
        }
        return result;
    }

    public async Task<AppResult<CampaignDataForDonorDTO?>> GetAllCampaignDataForDonorAsync()
    {
        var result = new AppResult<CampaignDataForDonorDTO?>();
        try
        {
            #region # Validate

            var currentCampaign = await _repo.GetCurrentlyActiveCampaignAsync();
            if (currentCampaign is null)
            {
                result.Failure(ErrorCode.BadRequest, "No active campaign found.");
                return result;
            }

            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var isAdopting = today >= currentCampaign.AdoptionStartDate && today <= currentCampaign.AdoptionEndDate;
            if (!isAdopting)
            {
                result.Failure(ErrorCode.BadRequest, "Adoption is ended.");
                return result;
            }

            #endregion

            #region # Execute

            var campaignData = new CampaignDataForDonorDTO
            {
                Id = currentCampaign.Id,
                Name = currentCampaign.Name,
                Instructions = currentCampaign.Instructions,
                Families = await _repo.GetAllFamiliesForDonorAsync(currentCampaign.Id)
            };

            result.Success(campaignData);

            #endregion
        }
        catch (Exception ex)
        {
            result.Failure(ErrorCode.InternalServerError, ex.Message);
        }
        return result;
    }

    public async Task<AppResult<Campaign?>> GetCampaignByIdAsync(long campaignId)
    {
        var result = new AppResult<Campaign?>();
        try
        {
            #region # Validate

            if (campaignId <= 0)
            {
                result.Failure(ErrorCode.BadRequest, "Please enter a valid campaignId.");
                return result;
            }

            #endregion

            #region # Execute

            var campaign = await _repo.GetCampaignByIdAsync(campaignId);
            if (campaign is null)
            {
                result.Failure(ErrorCode.NotFound, "No Campaign found for the given campaignId");
            }
            else
            {
                result.Success(campaign);
            }

            #endregion
        }
        catch (Exception ex)
        {
            result.Failure(ErrorCode.InternalServerError, ex.Message);
        }
        return result;
    }

    public async Task<AppResult> UpsertCampaignAsync(Campaign campaign)
    {
        var result = new AppResult();
        try
        {
            #region # Validate

            var hasError = result.Validate(campaign);
            if (hasError)
            {
                return result;
            }

            var campaignExists = await _repo.CampaignExistsByNameAsync(campaign.Name, campaign.Id);
            if (campaignExists)
            {
                result.Failure(ErrorCode.BadRequest, "A campaign with the same name already exists.");
                return result;
            }

            var isCampaignDateValid = await _repo.IsCampaignDateValidAsync(campaign.StartDate!.Value, campaign.EndDate!.Value, campaign.Id);
            if (!isCampaignDateValid)
            {
                result.Failure(ErrorCode.BadRequest, "A campaign already exists within the specified date range.");
                return result;
            }

            #endregion

            #region # Execute

            var isUpserted = await _repo.UpsertCampaignAsync(campaign);

            if (isUpserted is false)
            {
                result.Failure(ErrorCode.InternalServerError, "Something went wrong, Please try again later.");
            }
            else
            {
                result.Success();
            }

            #endregion
        }
        catch (Exception ex)
        {
            result.Failure(ErrorCode.InternalServerError, ex.Message);
        }
        return result;
    }

    public async Task<AppResult<Campaign?>> GetCurrentlyActiveCampaignAsync()
    {
        var result = new AppResult<Campaign?>();
        try
        {
            #region # Validate


            #endregion

            #region # Execute

            var campaign = await _repo.GetCurrentlyActiveCampaignAsync();
            if (campaign is null)
            {
                result.Failure(ErrorCode.NotFound, "No active campaign found.");
            }
            else
            {
                result.Success(campaign);
            }

            #endregion
        }
        catch (Exception ex)
        {
            result.Failure(ErrorCode.InternalServerError, ex.Message);
        }
        return result;
    }

    public async Task<AppResult<IEnumerable<Campaign>?>> GetAllCampaignsForDropdownAsync(long campaignId = 0)
    {
        var result = new AppResult<IEnumerable<Campaign>?>();
        try
        {
            #region # Validate


            #endregion

            #region # Execute

            var programDetails = await _repo.GetAllCampaignsForArchiveAsync(campaignId);

            result.Success(programDetails);

            #endregion
        }
        catch (Exception ex)
        {
            result.Failure(ErrorCode.InternalServerError, ex.Message);
        }
        return result;
    }

    #endregion

    #region # Program

    public async Task<AppResult<PaginatedList<Models.Program>>> GetAllProgramsAsync(int currentPage, int pageSize, string? searchTerm = null)
    {
        var result = new AppResult<PaginatedList<Models.Program>>();
        try
        {
            #region # Validate

            if (currentPage <= 0 || pageSize <= 0)
            {
                result.Failure(ErrorCode.BadRequest, "Invalid pagination parameters.");
                return result;
            }

            #endregion

            #region # Execute

            var paginatedProgram = await _repo.GetAllProgramsAsync(currentPage, pageSize, searchTerm);

            result.Success(paginatedProgram);

            #endregion
        }
        catch (Exception ex)
        {
            result.Failure(ErrorCode.InternalServerError, ex.Message);
        }
        return result;
    }

    public async Task<AppResult<Models.Program?>> GetProgramByIdAsync(long programId)
    {
        var result = new AppResult<Models.Program?>();
        try
        {
            #region # Validate

            if (programId <= 0)
            {
                result.Failure(ErrorCode.BadRequest, "Please enter a valid programId.");
                return result;
            }

            #endregion

            #region # Execute

            var program = await _repo.GetProgramByIdAsync(programId);

            if (program is null)
            {
                result.Failure(ErrorCode.NotFound, "No Program found for the given programId");
            }
            else
            {
                result.Success(program);
            }

            #endregion
        }
        catch (Exception ex)
        {
            result.Failure(ErrorCode.InternalServerError, ex.Message);
        }
        return result;
    }

    public async Task<AppResult> UpsertProgramAsync(Models.Program program)
    {
        var result = new AppResult();
        try
        {
            #region # Validate

            var hasError = result.Validate(program);
            if (hasError)
            {
                return result;
            }

            var programExists = await _repo.ProgramExistsByNameAsync(program.Name, program.Id);
            if (programExists)
            {
                result.Failure(ErrorCode.BadRequest, "A program with the same name already exists.");
                return result;
            }

            #endregion

            #region # Execute

            var isUpserted = await _repo.UpsertProgramAsync(program);

            if (isUpserted is false)
            {
                result.Failure(ErrorCode.InternalServerError, "Something went wrong, Please try again later.");
            }
            else
            {
                result.Success();
            }

            #endregion
        }
        catch (Exception ex)
        {
            result.Failure(ErrorCode.InternalServerError, ex.Message);
        }
        return result;
    }

    public async Task<AppResult<IEnumerable<Models.Program>?>> GetAllProgramsForDropdownAsync(long programId = 0)
    {
        var result = new AppResult<IEnumerable<Models.Program>?>();
        try
        {
            #region # Validate


            #endregion

            #region # Execute

            var programDetails = await _repo.GetAllProgramsForDropdownAsync(programId);

            result.Success(programDetails);

            #endregion
        }
        catch (Exception ex)
        {
            result.Failure(ErrorCode.InternalServerError, ex.Message);
        }
        return result;
    }

    #endregion

    #region # User

    public async Task<AppResult<PaginatedList<User>>> GetAllUsersAsync(int currentPage, int pageSize, string? searchTerm = null)
    {
        var result = new AppResult<PaginatedList<User>>();
        try
        {
            #region # Validate

            if (currentPage <= 0 || pageSize <= 0)
            {
                result.Failure(ErrorCode.BadRequest, "Invalid pagination parameters.");
                return result;
            }

            #endregion

            #region # Execute

            var paginatedUsers = await _repo.GetAllUsersAsync(currentPage, pageSize, searchTerm);

            result.Success(paginatedUsers);

            #endregion
        }
        catch (Exception ex)
        {
            result.Failure(ErrorCode.InternalServerError, ex.Message);
        }
        return result;
    }

    public async Task<AppResult<User?>> GetUserByIdAsync(long userId)
    {
        var result = new AppResult<User?>();
        try
        {
            #region # Validate

            if (userId <= 0)
            {
                result.Failure(ErrorCode.BadRequest, "Please enter a valid userId.");
                return result;
            }

            #endregion

            #region # Execute

            var user = await _repo.GetUserByIdAsync(userId);

            if (user is null)
            {
                result.Failure(ErrorCode.NotFound, "No User found for the given userId");
            }
            else
            {
                result.Success(user);
            }

            #endregion
        }
        catch (Exception ex)
        {
            result.Failure(ErrorCode.InternalServerError, ex.Message);
        }
        return result;
    }

    public async Task<AppResult> UpsertUserAsync(User user)
    {
        var result = new AppResult();
        try
        {
            #region # Validate

            var hasError = result.Validate(user);
            if (hasError)
            {
                return result;
            }

            var userExists = await _repo.UserExistsByEmailAsync(user.EmailId, user.Id);
            if (userExists)
            {
                result.Failure(ErrorCode.BadRequest, "A user with the same Email Id already exists.");
                return result;
            }

            #endregion

            #region # Execute

            var isUpserted = await _repo.UpsertUserAsync(user);

            if (isUpserted is false)
            {
                result.Failure(ErrorCode.InternalServerError, "Something went wrong, Please try again later.");
            }
            else
            {
                result.Success();
            }

            #endregion
        }
        catch (Exception ex)
        {
            result.Failure(ErrorCode.InternalServerError, ex.Message);
        }
        return result;
    }

    #endregion

    #region # Family

    public async Task<AppResult<PaginatedList<FamilyListForAdminDTO>>> GetAllFamiliesForAdminAsync(long campaignId, Status status, int currentPage, int pageSize, string? searchTerm = null)
    {
        var result = new AppResult<PaginatedList<FamilyListForAdminDTO>>();
        try
        {
            #region # Validate

            if (currentPage <= 0 || pageSize <= 0)
            {
                result.Failure(ErrorCode.BadRequest, "Invalid pagination parameters.");
                return result;
            }

            if (status != Status.Registered && status != Status.Adopted && status != Status.Recieved && status != Status.Distributed)
            {
                result.Failure(ErrorCode.BadRequest, "Invalid status. Status should be either 1 (Registered), 2 (Adopted), 3 (Recieved) or 4 (Distributed).");
                return result;
            }

            if (campaignId <= 0)
            {
                result.Failure(ErrorCode.BadRequest, "No active campaign found.");
                return result;
            }

            // Roll back to campaignId if checking currentcampaign here cause performance issue
            var currentCampaign = await _repo.GetCurrentlyActiveCampaignAsync();
            if (currentCampaign is null)
            {
                result.Failure(ErrorCode.BadRequest, "No active campaign found.");
                return result;
            }

            #endregion

            #region # Execute

            var paginatedFamilies = await _repo.GetAllFamiliesForAdminAsync(
                currentCampaign.Id, status,
                currentPage, pageSize, searchTerm);

            result.Success(paginatedFamilies);

            #endregion
        }
        catch (Exception ex)
        {
            result.Failure(ErrorCode.InternalServerError, ex.Message);
        }
        return result;
    }

    public async Task<AppResult<PaginatedList<FamilyListForAdvocateDTO>>> GetAllFamiliesForAdvocateAsync(long campaignId, Status status, int currentPage, int pageSize, string? searchTerm = null)
    {
        var result = new AppResult<PaginatedList<FamilyListForAdvocateDTO>>();
        try
        {
            #region # Validate

            if (currentPage <= 0 || pageSize <= 0)
            {
                result.Failure(ErrorCode.BadRequest, "Invalid pagination parameters.");
                return result;
            }

            if (status != Status.InProgress && status != Status.Registered)
            {
                result.Failure(ErrorCode.BadRequest, "Invalid status. Status should be either 1 (InProgress) or 2 (Registered).");
                return result;
            }

            if (campaignId <= 0)
            {
                result.Failure(ErrorCode.BadRequest, "No active campaign found.");
                return result;
            }

            // Roll back to campaignId if checking currentcampaign here cause performance issue
            var currentCampaign = await _repo.GetCurrentlyActiveCampaignAsync();
            if (currentCampaign is null)
            {
                result.Failure(ErrorCode.BadRequest, "No active campaign found.");
                return result;
            }

            #endregion  

            #region # Execute

            var paginatedFamilies = await _repo.GetAllFamiliesForAdvocateAsync(
                currentCampaign.Id, status,
                currentPage, pageSize, searchTerm);

            result.Success(paginatedFamilies);

            #endregion
        }
        catch (Exception ex)
        {
            result.Failure(ErrorCode.InternalServerError, ex.Message);
        }
        return result;
    }

    public async Task<AppResult<Family?>> GetFamilyWithDetailsForAdminByFamilyIdAsync(long familyId)
    {
        var result = new AppResult<Family?>();
        try
        {
            #region # Validate

            if (familyId <= 0)
            {
                result.Failure(ErrorCode.BadRequest, "Please enter a valid familyId.");
                return result;
            }

            #endregion

            #region # Execute

            var family = await _repo.GetFamilyWithDetailsForAdminByFamilyIdAsync(familyId);

            if (family is null)
            {
                result.Failure(ErrorCode.NotFound, "No Family found for the given familyId");
            }
            else
            {
                result.Success(family);
            }

            #endregion
        }
        catch (Exception ex)
        {
            result.Failure(ErrorCode.InternalServerError, ex.Message);
        }
        return result;
    }

    public async Task<AppResult<Family?>> GetFamilyWithDetailsForAdvocateByFamilyIdAsync(long familyId)
    {
        var result = new AppResult<Family?>();
        try
        {
            #region # Validate

            if (familyId <= 0)
            {
                result.Failure(ErrorCode.BadRequest, "Please enter a valid familyId.");
                return result;
            }

            #endregion

            #region # Execute

            var family = await _repo.GetFamilyWithDetailsForAdvocateByFamilyIdAsync(familyId);

            if (family is null)
            {
                result.Failure(ErrorCode.NotFound, "No Family found for the given familyId");
            }
            else
            {
                result.Success(family);
            }

            #endregion
        }
        catch (Exception ex)
        {
            result.Failure(ErrorCode.InternalServerError, ex.Message);
        }
        return result;
    }

    public async Task<AppResult<Family?>> GetFamilyWithDetailsForDonorByFamilyIdAsync(long familyId)
    {
        var result = new AppResult<Family?>();
        try
        {
            #region # Validate

            if (familyId <= 0)
            {
                result.Failure(ErrorCode.BadRequest, "Please enter a valid familyId.");
                return result;
            }

            #endregion

            #region # Execute

            var family = await _repo.GetFamilyWithDetailsForDonorByFamilyIdAsync(familyId);

            if (family is null)
            {
                result.Failure(ErrorCode.NotFound, "No Family found for the given familyId");
            }
            else
            {
                result.Success(family);
            }

            #endregion
        }
        catch (Exception ex)
        {
            result.Failure(ErrorCode.InternalServerError, ex.Message);
        }
        return result;
    }

    public async Task<AppResult<Family?>> GetFamilyByIdAsync(long familyId)
    {
        var result = new AppResult<Family?>();
        try
        {
            #region # Validate

            if (familyId <= 0)
            {
                result.Failure(ErrorCode.BadRequest, "Please enter a valid familyId.");
                return result;
            }

            #endregion

            #region # Execute

            var family = await _repo.GetFamilyByIdAsync(familyId);

            if (family is null)
            {
                result.Failure(ErrorCode.NotFound, "No Family found for the given familyId");
            }
            else
            {
                result.Success(family);
            }

            #endregion
        }
        catch (Exception ex)
        {
            result.Failure(ErrorCode.InternalServerError, ex.Message);
        }
        return result;
    }

    public async Task<AppResult> UpsertFamilyByAdminAsync(Family family)
    {
        var result = new AppResult();
        try
        {
            #region # Validate

            var hasError = result.Validate(family);
            if (hasError)
            {
                return result;
            }

            var currentCampaign = await _repo.GetCurrentlyActiveCampaignAsync();
            if (currentCampaign is null)
            {
                result.Failure(ErrorCode.BadRequest, "No active campaign found.");
                return result;
            }

            if (family.Id > 0)
            {
                if (family.CampaignId <= 0 || (family.CampaignId != currentCampaign.Id))
                {
                    result.Failure(ErrorCode.BadRequest, "Campaign mismatch error.");
                    return result;
                }
            }
            else
            {
                family.CampaignId = currentCampaign.Id;
            }

            var clientCodeExists = await _repo.FamilyExistsByClientCodeAsync(family.ClientCode, family.Id);
            if (clientCodeExists)
            {
                result.Failure(ErrorCode.BadRequest, "A family with the same client code already exists.");
                return result;
            }

            var salesForceNumberExists = await _repo.FamilyExistsBySalesForcenumberAsync(family.SalesForceNumber, family.Id);
            if (salesForceNumberExists)
            {
                result.Failure(ErrorCode.BadRequest, "A family with the same salesforce number already exists.");
                return result;
            }

            #endregion

            #region # Execute

            family.MealKitNotes = family.NeedMealKit ? family.MealKitNotes : null;

            var isUpserted = await _repo.UpsertFamilyAsync(family);

            if (isUpserted is false)
            {
                result.Failure(ErrorCode.InternalServerError, "Something went wrong, Please try again later.");
            }
            else
            {
                result.Success();
            }

            #endregion
        }
        catch (Exception ex)
        {
            result.Failure(ErrorCode.InternalServerError, ex.Message);
        }
        return result;
    }

    public async Task<AppResult> UpsertFamilyByAdvocateAsync(Family family)
    {
        var result = new AppResult();
        try
        {
            #region # Validate

            var hasError = result.Validate(family);
            if (hasError)
            {
                return result;
            }

            var currentCampaign = await _repo.GetCurrentlyActiveCampaignAsync();
            if (currentCampaign is null)
            {
                result.Failure(ErrorCode.BadRequest, "No active campaign found.");
                return result;
            }

            var isDataCollectionStarted = DateOnly.FromDateTime(DateTime.UtcNow) < currentCampaign.DataCollectionStartDate;
            if (isDataCollectionStarted)
            {
                result.Failure(ErrorCode.BadRequest, "Data collection is not yet started.");

                return result;
            }

            var isDataCollectionEnded = DateOnly.FromDateTime(DateTime.UtcNow) > currentCampaign.DataCollectionEndDate;
            if (isDataCollectionEnded)
            {
                result.Failure(ErrorCode.BadRequest, "Data collection is ended.");
                return result;
            }

            if (family.Id > 0)
            {
                if (family.CampaignId <= 0 || (family.CampaignId != currentCampaign.Id))
                {
                    result.Failure(ErrorCode.BadRequest, "Campaign mismatch error.");
                    return result;
                }
            }
            else
            {
                family.CampaignId = currentCampaign.Id;
            }

            var clientCodeExists = await _repo.FamilyExistsByClientCodeAsync(family.ClientCode, family.Id);
            if (clientCodeExists)
            {
                result.Failure(ErrorCode.BadRequest, "A family with the same client code already exists.");
                return result;
            }

            var salesForceNumberExists = await _repo.FamilyExistsBySalesForcenumberAsync(family.SalesForceNumber, family.Id);
            if (salesForceNumberExists)
            {
                result.Failure(ErrorCode.BadRequest, "A family with the same salesforce number already exists.");
                return result;
            }

            #endregion

            #region # Execute

            family.MealKitNotes = family.NeedMealKit ? family.MealKitNotes : null;

            var isUpserted = await _repo.UpsertFamilyAsync(family);

            if (isUpserted is false)
            {
                result.Failure(ErrorCode.InternalServerError, "Something went wrong, Please try again later.");
            }
            else
            {
                result.Success();
            }

            #endregion
        }
        catch (Exception ex)
        {
            result.Failure(ErrorCode.InternalServerError, ex.Message);
        }
        return result;
    }

    public async Task<AppResult> UpdateFamilyRegistrationStatusAsync(long familyId, Status status)
    {
        var result = new AppResult();
        try
        {
            #region # Validate

            if (familyId <= 0)
            {
                result.Failure(ErrorCode.BadRequest, "Please enter a valid familyId.");
                return result;
            }

            var currentCampaign = await _repo.GetCurrentlyActiveCampaignAsync();
            if (currentCampaign is null)
            {
                result.Failure(ErrorCode.BadRequest, "No active campaign found.");
                return result;
            }

            #endregion

            #region # Execute

            var isStatusUpdated = await _repo.UpdateFamilyRegistrationStatusAsync(familyId, status);

            if (!isStatusUpdated)
            {
                result.Failure(ErrorCode.InternalServerError, "Something went wrong, Please try again later.");
            }
            else
            {
                result.Success();
            }

            #endregion
        }
        catch (Exception ex)
        {
            result.Failure(ErrorCode.InternalServerError, ex.Message);
        }
        return result;
    }

    public async Task<AppResult> AcknowledgeFamilyForAdvocateByFamilyIdAsync(long familyId, Status status)
    {
        var result = new AppResult();
        try
        {
            #region # Validate

            if (familyId <= 0)
            {
                result.Failure(ErrorCode.BadRequest, "Please enter a valid familyId.");
                return result;
            }

            var currentCampaign = await _repo.GetCurrentlyActiveCampaignAsync();
            if (currentCampaign is null)
            {
                result.Failure(ErrorCode.BadRequest, "No active campaign found.");
                return result;
            }

            var isDataCollectionStarted = DateOnly.FromDateTime(DateTime.UtcNow) < currentCampaign.DataCollectionStartDate;
            if (isDataCollectionStarted)
            {
                result.Failure(ErrorCode.BadRequest, "Data collection is not yet started.");

                return result;
            }

            var isDataCollectionEnded = DateOnly.FromDateTime(DateTime.UtcNow) > currentCampaign.DataCollectionEndDate;
            if (isDataCollectionEnded)
            {
                result.Failure(ErrorCode.BadRequest, "Data collection is ended.");
                return result;
            }

            #endregion

            #region # Execute

            var isAcknowledge = await _repo.UpdateFamilyRegistrationStatusAsync(familyId, status);

            if (isAcknowledge is false)
            {
                result.Failure(ErrorCode.InternalServerError, "Something went wrong, Please try again later.");
            }
            else
            {
                result.Success();
            }

            #endregion
        }
        catch (Exception ex)
        {
            result.Failure(ErrorCode.InternalServerError, ex.Message);
        }
        return result;
    }

    public async Task<AppResult<byte[]>> ExportFamilyDataForAdminAsync(Status status)
    {
        var result = new AppResult<byte[]>();
        try
        {
            #region # Validate

            var currentCampaign = await _repo.GetCurrentlyActiveCampaignAsync();
            if (currentCampaign is null)
            {
                result.Failure(ErrorCode.BadRequest, "Right now campaign is active");
                return result;
            }

            #endregion

            #region # Execute

            var exportableData = await _repo.GetAllFamilyDataForExportAsync(currentCampaign.Id, status);
            if (exportableData == null || exportableData.Count <= 0)
            {
                result.Failure(ErrorCode.BadRequest, "Nothing to export.");
                return result;
            }

            var byteArray = ExcelHelper.ExportFamilyDataForAdmin(exportableData);
            if (byteArray == null || byteArray.Length <= 0)
            {
                result.Failure(ErrorCode.InternalServerError, "Something went wrong while exporting the data.");
                return result;
            }

            result.Success(byteArray);

            #endregion
        }
        catch (Exception ex)
        {
            result.Failure(ErrorCode.InternalServerError, ex.Message);
        }
        return result;
    }

    public async Task<AppResult> DeleteFamilyForAdminByIdAsync(long familyId)
    {
        var result = new AppResult();
        try
        {
            #region # Validate

            if (familyId <= 0)
            {
                result.Failure(ErrorCode.BadRequest, "Please enter a valid userId.");
                return result;
            }

            #endregion

            #region # Execute

            var isDeleted = await _repo.DeleteFamilyForAdminByIdAsync(familyId);

            if (isDeleted is false)
            {
                result.Failure(ErrorCode.InternalServerError, "Something went wrong, Please try again later.");
            }
            else
            {
                result.Success();
            }

            #endregion
        }
        catch (Exception ex)
        {
            result.Failure(ErrorCode.InternalServerError, ex.Message);
        }
        return result;
    }

    #endregion

    #region # Family Members

    public async Task<AppResult<IEnumerable<FamilyMember>>> GetAllFamilyMembersByFamilyIdAsync(long familyId)
    {
        var result = new AppResult<IEnumerable<FamilyMember>>();
        try
        {
            #region # Validate

            if (familyId <= 0)
            {
                result.Failure(ErrorCode.BadRequest, "Please enter a valid familyId.");
                return result;
            }

            #endregion

            #region # Execute

            var familyMembers = await _repo.GetAllFamilyMembersByFamilyIdAsync(familyId);

            result.Success(familyMembers);

            #endregion
        }
        catch (Exception ex)
        {
            result.Failure(ErrorCode.InternalServerError, ex.Message);
        }
        return result;
    }

    public async Task<AppResult> UpsertFamilyMembersAsync(long familyId, List<FamilyMember> familyMembers)
    {
        var result = new AppResult();
        try
        {
            #region # Validate

            if (familyId <= 0)
            {
                result.Failure(ErrorCode.BadRequest, "Please enter a valid familyId.");
                return result;
            }

            if (familyMembers is not null && familyMembers.Count > 0)
            {
                foreach (var familyMember in familyMembers)
                {
                    var hasError = result.Validate(familyMember);
                    if (hasError)
                    {
                        return result;
                    }
                }
            }

            #endregion

            #region # Execute

            var isUpserted = await _repo.UpsertFamilyMembersAsync(familyId, familyMembers);

            if (isUpserted is false)
            {
                result.Failure(ErrorCode.InternalServerError, "Something went wrong, Please try again later.");
            }
            else
            {
                result.Success();
            }

            #endregion
        }
        catch (Exception ex)
        {
            result.Failure(ErrorCode.InternalServerError, ex.Message);
        }
        return result;
    }

    #endregion

    #region # Donor

    public async Task<AppResult<Donor?>> GetDonorByFamilyIdAsync(long familyId)
    {
        var result = new AppResult<Donor?>();
        try
        {
            #region # Validate

            if (familyId <= 0)
            {
                result.Failure(ErrorCode.BadRequest, "Please select a family.");
                return result;
            }

            #endregion

            #region # Execute

            var donor = await _repo.GetDonorByFamilyIdAsync(familyId);
            if (donor is null)
            {
                result.Failure(ErrorCode.BadRequest, "Donor details not found");
            }
            else
            {
                result.Success(donor);
            }

            #endregion
        }
        catch (Exception ex)
        {
            result.Failure(ErrorCode.InternalServerError, ex.Message);
        }
        return result;
    }

    public async Task<AppResult<List<long>?>> GetAlreadyAdoptedFamilyIdsAsync(List<long> familyIds)
    {
        var result = new AppResult<List<long>?>();
        try
        {
            #region # Validate

            var hasSelectedFamilies = familyIds.Count > 0;
            if (!hasSelectedFamilies)
            {
                result.Failure(ErrorCode.BadRequest, "You should simply select atleast one family for adoption.");
                return result;
            }

            #endregion

            #region # Execute

            var alreadyAdoptedFamilyIds = await _repo.GetAlreadyAdoptedFamilyIdsAsync(familyIds);

            result.Success(alreadyAdoptedFamilyIds);

            #endregion
        }
        catch (Exception ex)
        {
            result.Failure(ErrorCode.InternalServerError, ex.Message);
        }
        return result;
    }

    public async Task<AppResult> AdoptFamiliesByDonorAsync(Donor donor, List<long> selectedFamilyIds)
    {
        var result = new AppResult();
        var transaction = await _ctx.Database.BeginTransactionAsync();
        try
        {
            #region # Validate

            var hasError = result.Validate(donor);
            if (hasError)
            {
                return result;
            }

            var hasSelectedFamilies = selectedFamilyIds.Count > 0;
            if (!hasSelectedFamilies)
            {
                result.Failure(ErrorCode.BadRequest, "You should simply select atleast one family for adoption.");
                return result;
            }

            var currentCampaign = await _repo.GetCurrentlyActiveCampaignAsync();
            if (currentCampaign is null)
            {
                result.Failure(ErrorCode.BadRequest, "No active campaign found.");
                return result;
            }

            var isAdoptionStarted = DateOnly.FromDateTime(DateTime.UtcNow) < currentCampaign.AdoptionStartDate;
            if (isAdoptionStarted)
            {
                result.Failure(ErrorCode.BadRequest, "Adoption is not yet started.");
                return result;
            }

            var isAdoptionEnded = DateOnly.FromDateTime(DateTime.UtcNow) > currentCampaign.AdoptionEndDate;
            if (isAdoptionEnded)
            {
                result.Failure(ErrorCode.BadRequest, "Adoption is ended.");
                return result;
            }

            var alreadyAdoptedFamilyIds = await _repo.GetAlreadyAdoptedFamilyIdsAsync(selectedFamilyIds);
            if (alreadyAdoptedFamilyIds != null && alreadyAdoptedFamilyIds.Count > 0)
            {
                result.Failure(ErrorCode.BadRequest, "Some of the selected families have already been adopted.");
                return result;
            }

            #endregion

            #region # Execute

            var isDonorSaved = await _repo.UpsertDonorAsync(donor);
            if (!isDonorSaved && donor.Id <= 0)
            {
                result.Failure(ErrorCode.InternalServerError, "Something went wront while saving the Donor");
                return result;
            }

            var areFamiliesMarkedAsAdopted = await _repo.MarkFamiliesAsAdopted(donor.Id, selectedFamilyIds);
            if (!areFamiliesMarkedAsAdopted)
            {
                result.Failure(ErrorCode.InternalServerError, "Something went wront while adopting families.");
                return result;
            }

            await _mailer.SendAdoptionMailAsync(donor, currentCampaign);

            result.Success();
            await transaction.CommitAsync();

            #endregion
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            result.Failure(ErrorCode.InternalServerError, ex.Message);
        }
        return result;
    }

    public async Task<AppResult> UpdateDonorDetailsAndSendAdoptionMailAsync(Donor donor)
    {
        var result = new AppResult();
        try
        {
            #region # Validate

            var hasError = result.Validate(donor);
            if (hasError)
            {
                return result;
            }

            var currentCampaign = await _repo.GetCurrentlyActiveCampaignAsync();
            if (currentCampaign is null)
            {
                result.Failure(ErrorCode.BadRequest, "No active campaign found.");
                return result;
            }

            #endregion

            #region # Execute

            var isDonorUpdated = await _repo.UpsertDonorAsync(donor);
            if (!isDonorUpdated)
            {
                result.Failure(ErrorCode.BadRequest, "Failed to update donor details.");
                return result;
            }

            await _mailer.SendAdoptionMailAsync(donor, currentCampaign);

            result.Success();

            #endregion
        }
        catch (Exception ex)
        {
            result.Failure(ErrorCode.InternalServerError, ex.Message);
        }
        return result;
    }

    #endregion

    #region # Dashboard

    public async Task<AppResult<DashboardDetailsDTO>> GetDashboardDetailsAsync()
    {
        var result = new AppResult<DashboardDetailsDTO>();
        try
        {
            #region # Validate

            var currentCampaign = await _repo.GetCurrentlyActiveCampaignAsync();
            if (currentCampaign is null)
            {
                result.Failure(ErrorCode.BadRequest, "No active campaign found.");
                return result;
            }

            #endregion

            #region # Execute

            var dashboardDetails = await _repo.GetDashboardDetailsAsync(currentCampaign.Id) ?? new DashboardDetailsDTO();

            result.Success(dashboardDetails);

            #endregion
        }
        catch (Exception ex)
        {
            result.Failure(ErrorCode.InternalServerError, ex.Message);
        }
        return result;
    }

    #endregion

    #region # Late Registration

    public async Task<AppResult<PaginatedList<LateRegistration>>> GetAllLateRegistrationsForAdvocateAsync(int currentPage, int pageSize, string? searchTerm = null)
    {
        var result = new AppResult<PaginatedList<LateRegistration>>();
        try
        {
            #region # Validate

            if (currentPage <= 0 || pageSize <= 0)
            {
                result.Failure(ErrorCode.BadRequest, "Invalid pagination parameters.");
                return result;
            }

            var currentCampaign = await _repo.GetCurrentlyActiveCampaignAsync();
            if (currentCampaign is null)
            {
                result.Failure(ErrorCode.BadRequest, "No active campaign found.");
                return result;
            }

            #endregion

            #region # Execute

            var paginatedLateRegistration = await _repo.GetAllLateRegistrationsForAdvocateAsync(currentCampaign.Id, currentPage, pageSize, searchTerm);

            result.Success(paginatedLateRegistration);

            #endregion
        }
        catch (Exception ex)
        {
            result.Failure(ErrorCode.InternalServerError, ex.Message);
        }
        return result;
    }

    public async Task<AppResult<PaginatedList<LateRegistration>>> GetAllLateRegistrationsForAdminAsync(int currentPage, int pageSize, string? searchTerm = null)
    {
        var result = new AppResult<PaginatedList<LateRegistration>>();
        try
        {
            #region # Validate

            if (currentPage <= 0 || pageSize <= 0)
            {
                result.Failure(ErrorCode.BadRequest, "Invalid pagination parameters.");
                return result;
            }

            var currentCampaign = await _repo.GetCurrentlyActiveCampaignAsync();
            if (currentCampaign is null)
            {
                result.Failure(ErrorCode.BadRequest, "No active campaign found.");
                return result;
            }

            #endregion

            #region # Execute

            var paginatedLateRegistration = await _repo.GetAllLateRegistrationsForAdminAsync(currentCampaign.Id, currentPage, pageSize, searchTerm);

            result.Success(paginatedLateRegistration);

            #endregion
        }
        catch (Exception ex)
        {
            result.Failure(ErrorCode.InternalServerError, ex.Message);
        }
        return result;
    }

    public async Task<AppResult<LateRegistration?>> GetLateRegistrationByIdAsync(long registrationId)
    {
        var result = new AppResult<LateRegistration?>();
        try
        {
            #region # Validate

            if (registrationId <= 0)
            {
                result.Failure(ErrorCode.BadRequest, "Please enter a valid registrationId.");
                return result;
            }

            #endregion

            #region # Execute

            var lateRegistration = await _repo.GetLateRegistrationByIdAsync(registrationId);
            if (lateRegistration is null)
            {
                result.Failure(ErrorCode.NotFound, "No late registration found for the given registrationId.");
            }
            else
            {
                result.Success(lateRegistration);
            }

            #endregion
        }
        catch (Exception ex)
        {
            result.Failure(ErrorCode.InternalServerError, ex.Message);
        }
        return result;
    }

    public async Task<AppResult> UpsertLateRegistrationAsync(LateRegistration lateRegistration)
    {
        var result = new AppResult();
        try
        {
            #region # Validate

            var hasError = result.Validate(lateRegistration);
            if (hasError)
            {
                return result;
            }

            var currentCampaign = await _repo.GetCurrentlyActiveCampaignAsync();
            if (currentCampaign is null)
            {
                result.Failure(ErrorCode.BadRequest, "No active campaign found.");
                return result;
            }

            if (lateRegistration.Id > 0)
            {
                if (lateRegistration.CampaignId <= 0 || (lateRegistration.CampaignId != currentCampaign.Id))
                {
                    result.Failure(ErrorCode.BadRequest, "Campaign mismatch error.");
                    return result;
                }
            }
            else
            {
                lateRegistration.CampaignId = currentCampaign.Id;
            }

            #endregion

            #region # Execute

            var isUpserted = await _repo.UpsertLateRegistrationAsync(lateRegistration);
            if (isUpserted is false)
            {
                result.Failure(ErrorCode.InternalServerError, "Something went wrong, Please try again later.");
            }
            else
            {
                result.Success();
            }

            #endregion
        }
        catch (Exception ex)
        {
            result.Failure(ErrorCode.InternalServerError, ex.Message);
        }
        return result;
    }

    public async Task<AppResult<byte[]>> ExportLateRegistrationAsync()
    {
        var result = new AppResult<byte[]>();
        try
        {
            #region # Validate

            var currentCampaign = await _repo.GetCurrentlyActiveCampaignAsync();
            if (currentCampaign is null)
            {
                result.Failure(ErrorCode.BadRequest, "No active campaign found.");
                return result;
            }

            #endregion

            #region # Execute

            var exportableData = await _repo.GetAllLateRegistrationDataForExportAsync(currentCampaign.Id);
            if (exportableData == null || exportableData.Count <= 0)
            {
                result.Failure(ErrorCode.BadRequest, "Nothing to export.");
                return result;
            }

            var byteArray = ExcelHelper.ExportLateRegistrationDataForAdmin(exportableData, currentCampaign.Name);
            if (byteArray == null || byteArray.Length <= 0)
            {
                result.Failure(ErrorCode.InternalServerError, "Something went wrong while exporting the data.");
                return result;
            }

            result.Success(byteArray);

            #endregion
        }
        catch (Exception ex)
        {
            result.Failure(ErrorCode.InternalServerError, ex.Message);
        }
        return result;
    }

    #endregion

    #region # Archive

    public async Task<AppResult<byte[]>> ExportArchiveDataForAdminAsync(DataFilter status, long campaignId)
    {
        var result = new AppResult<byte[]>();
        try
        {
            #region # Validate

            if (campaignId == 0)
            {
                result.Failure(ErrorCode.BadRequest, "Please select a campaign.");
                return result;
            }

            if (DataFilter.None == status)
            {
                result.Failure(ErrorCode.BadRequest, "Please select data filter.");
                return result;
            }

            var currentCampaign = await _repo.GetAllCampaignsForArchiveAsync(campaignId);
            if (currentCampaign is null)
            {
                result.Failure(ErrorCode.BadRequest, "No active campaign found.");
                return result;
            }

            #endregion

            #region # Execute

            if (status == DataFilter.LateRegistration)
            {
                var lateRgistrationData = await _repo.GetAllLateRegistrationDataForArchiveExportAsync(campaignId);
                if (lateRgistrationData == null || lateRgistrationData.Count <= 0)
                {
                    result.Failure(ErrorCode.BadRequest, "Nothing to export.");
                    return result;
                }

                var byteArray = ExcelHelper.ExportLateRegistrationDataForAdmin(lateRgistrationData, currentCampaign[0].Name);
                if (byteArray == null || byteArray.Length <= 0)
                {
                    result.Failure(ErrorCode.InternalServerError, "Something went wrong while exporting the data.");
                    return result;
                }
                result.Success(byteArray);
            }
            else
            {
                var _status = Status.None;
                if (status == DataFilter.AdoptionNeeded)
                {
                    _status = Status.Registered;
                }
                if (status == DataFilter.Adopted)
                {
                    _status = Status.Adopted;
                }
                if (status == DataFilter.Recieved)
                {
                    _status = Status.Recieved;
                }
                if (status == DataFilter.Distributed)
                {
                    _status = Status.Distributed;
                }

                var exportableData = await _repo.GetAllFamilyDataForExportAsync(campaignId, _status);
                if (exportableData == null || exportableData.Count <= 0)
                {
                    result.Failure(ErrorCode.BadRequest, "Nothing to export.");
                    return result;
                }

                var byteArray = ExcelHelper.ExportFamilyDataForAdmin(exportableData);
                if (byteArray == null || byteArray.Length <= 0)
                {
                    result.Failure(ErrorCode.InternalServerError, "Something went wrong while exporting the data.");
                    return result;
                }
                result.Success(byteArray);
            }

            #endregion
        }
        catch (Exception ex)
        {
            result.Failure(ErrorCode.InternalServerError, ex.Message);
        }
        return result;
    }

    #endregion
}
