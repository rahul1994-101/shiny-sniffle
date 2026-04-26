using AAF.Models;
using AAF.Utilities;

using Dapper;

using Microsoft.EntityFrameworkCore;

using System.Data;

namespace AAF.Data;

public sealed class Persistence
{
    #region # Init 

    public Persistence(AppDbContext ctx, AuthService authService)
    {
        _ctx = ctx;
        _authService = authService;
    }

    private readonly AppDbContext _ctx;
    private readonly AuthService _authService;

    #endregion

    public async Task<User?> SignInAsync(SignInRequest signInRequest)
    {
        var encPassword = signInRequest.Password.Encrypt();

        return await _ctx.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.EmailId.ToLower() == signInRequest.EmailId.ToLower() &&
                x.Password.ToLower() == encPassword &&
                x.IsActive == true
            );
    }

    public async Task<bool> ChangePasswordAsync(long userId, string newPassword)
    {
        ArgumentException.ThrowIfNullOrEmpty(newPassword, nameof(newPassword));
        if (userId <= 0)
        {
            throw new ArgumentException("Invalid userId.", nameof(userId));
        }

        var loggedInUserId = await _authService.GetLoggedInUserIdAsync();
        var currentTime = DateTime.UtcNow;

        var user = new User
        {
            Id = userId,
            Password = newPassword,

            UpdatedBy = loggedInUserId,
            UpdatedOn = currentTime
        };

        _ctx.Users.Attach(user);

        _ctx.Entry(user).Property(u => u.Password).IsModified = true;
        _ctx.Entry(user).Property(u => u.UpdatedBy).IsModified = true;
        _ctx.Entry(user).Property(u => u.UpdatedOn).IsModified = true;

        var rowsAffected = await _ctx.SaveChangesAsync();
        DetachEntity(user);
        return rowsAffected > 0;
    }

    #region # Campaign

    public async Task<PaginatedList<Campaign>> GetAllCampaignsAsync(int currentPage, int pageSize, string? searchTerm = null)
    {
        var SP = DBConstant.SP.usp_GetAllCampaigns;
        var P = new DynamicParameters();

        P.Add("@CurrentPage", currentPage);
        P.Add("@PageSize", pageSize);
        P.Add("@SearchTerm", searchTerm);

        var dapperResp = await _ctx.Connection
            .QueryMultipleAsync(SP, P, null, null, commandType: CommandType.StoredProcedure);

        var records = (await dapperResp.ReadAsync<Campaign>()).ToList();
        var totalRecords = await dapperResp.ReadSingleAsync<int>();

        return new PaginatedList<Campaign>
        {
            PageSize = pageSize,
            CurrentPage = currentPage,
            TotalPages = (int)Math.Ceiling(totalRecords / (double)pageSize),
            TotalRecords = totalRecords,
            Records = records
        };
    }

    public async Task<Campaign?> GetCampaignByIdAsync(long campaignId, bool track = false)
    {
        var query = _ctx.Campaigns.AsQueryable();

        if (track is false)
        {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync(x => x.Id == campaignId);
    }

    public async Task<bool> UpsertCampaignAsync(Campaign campaign)
    {
        ArgumentNullException.ThrowIfNull(campaign);

        var loggedInUserId = await _authService.GetLoggedInUserIdAsync();
        var currentTime = DateTime.UtcNow;

        if (campaign.Id <= 0)
        {
            campaign.CreatedBy = loggedInUserId;
            campaign.CreatedOn = currentTime;

            await _ctx.Campaigns.AddAsync(campaign);
        }
        else
        {
            campaign.UpdatedBy = loggedInUserId;
            campaign.UpdatedOn = currentTime;

            _ctx.Campaigns.Attach(campaign);

            _ctx.Entry(campaign).Property(u => u.Name).IsModified = true;
            _ctx.Entry(campaign).Property(u => u.Instructions).IsModified = true;
            _ctx.Entry(campaign).Property(u => u.DropOffInstructions).IsModified = true;
            _ctx.Entry(campaign).Property(u => u.StartDate).IsModified = true;
            _ctx.Entry(campaign).Property(u => u.EndDate).IsModified = true;
            _ctx.Entry(campaign).Property(u => u.DataCollectionStartDate).IsModified = true;
            _ctx.Entry(campaign).Property(u => u.DataCollectionEndDate).IsModified = true;
            _ctx.Entry(campaign).Property(u => u.AdoptionStartDate).IsModified = true;
            _ctx.Entry(campaign).Property(u => u.AdoptionEndDate).IsModified = true;
            _ctx.Entry(campaign).Property(u => u.DropOffStartDate).IsModified = true;
            _ctx.Entry(campaign).Property(u => u.DropOffEndDate).IsModified = true;
            _ctx.Entry(campaign).Property(u => u.DistributionStartDate).IsModified = true;
            _ctx.Entry(campaign).Property(u => u.DistributionEndDate).IsModified = true;
            _ctx.Entry(campaign).Property(u => u.IsActive).IsModified = true;
            _ctx.Entry(campaign).Property(u => u.UpdatedBy).IsModified = true;
            _ctx.Entry(campaign).Property(u => u.UpdatedOn).IsModified = true;
        }

        var rowsAffected = await _ctx.SaveChangesAsync();
        DetachEntity(campaign);
        return rowsAffected > 0;
    }


    public async Task<bool> CampaignExistsByNameAsync(string name, long campaignId = 0)
    {
        return await _ctx.Campaigns
            .AsNoTracking()
            .AnyAsync(x =>
                x.Name.ToLower() == name.ToLower() &&
                (campaignId == 0 || x.Id != campaignId)
            );
    }

    public async Task<bool> IsCampaignDateValidAsync(DateOnly startDate, DateOnly endDate, long campaignId = 0)
    {
        var isOverlapping = await _ctx.Campaigns
            .AsNoTracking()
            .AnyAsync(x =>
                (campaignId == 0 || x.Id != campaignId) &&
                (
                    (startDate >= x.StartDate && startDate <= x.EndDate) ||  // New start date is within an existing campaign
                    (endDate >= x.StartDate && endDate <= x.EndDate) ||      // New end date is within an existing campaign
                    (startDate <= x.StartDate && endDate >= x.EndDate)       // New campaign completely overlaps an existing campaign
                )
            );

        return !isOverlapping;
    }

    public async Task<Campaign?> GetCurrentlyActiveCampaignAsync()
    {
        var query = _ctx.Campaigns
            .AsQueryable()
            .AsNoTracking();

        var currentDate = DateOnly.FromDateTime(DateTime.UtcNow);
        var campaign = await query.FirstOrDefaultAsync(c => c.IsActive && c.StartDate <= currentDate && c.EndDate >= currentDate);

        return campaign;
    }

    public async Task<List<Campaign>?> GetAllCampaignsForArchiveAsync(long campaignId = 0)
    {
        var currentDate = DateOnly.FromDateTime(DateTime.UtcNow);

        var query = await _ctx.Campaigns
            .AsNoTracking()
            .Where(c => (c.IsActive && c.EndDate < currentDate) || (campaignId > 0 && c.Id == campaignId))
            .Select(c => new Campaign
            {
                Id = c.Id,
                Name = c.Name
            })
            .ToListAsync();

        return query;
    }

    #endregion

    #region # Program

    public async Task<PaginatedList<Models.Program>> GetAllProgramsAsync(int currentPage, int pageSize, string? searchTerm = null)
    {
        var SP = DBConstant.SP.usp_GetAllPrograms;
        var P = new DynamicParameters();

        P.Add("@CurrentPage", currentPage);
        P.Add("@PageSize", pageSize);
        P.Add("@SearchTerm", searchTerm);

        var dapperResp = await _ctx.Connection
            .QueryMultipleAsync(SP, P, null, null, commandType: CommandType.StoredProcedure);

        var records = (await dapperResp.ReadAsync<Models.Program>()).ToList();
        var totalRecords = await dapperResp.ReadSingleAsync<int>();

        return new PaginatedList<Models.Program>
        {
            PageSize = pageSize,
            CurrentPage = currentPage,
            TotalPages = (int)Math.Ceiling(totalRecords / (double)pageSize),
            TotalRecords = totalRecords,
            Records = records
        };
    }

    public async Task<Models.Program?> GetProgramByIdAsync(long programId, bool track = false)
    {
        var query = _ctx.Programs.AsQueryable();

        if (track is false)
        {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync(x => x.Id == programId);
    }

    public async Task<bool> UpsertProgramAsync(Models.Program program)
    {
        ArgumentNullException.ThrowIfNull(program);

        var loggedInUserId = await _authService.GetLoggedInUserIdAsync();
        var currentTime = DateTime.UtcNow;

        if (program.Id <= 0)
        {
            program.CreatedBy = loggedInUserId;
            program.CreatedOn = currentTime;

            await _ctx.Programs.AddAsync(program);
        }
        else
        {
            program.UpdatedBy = loggedInUserId;
            program.UpdatedOn = currentTime;

            _ctx.Programs.Attach(program);

            _ctx.Entry(program).Property(u => u.Name).IsModified = true;
            _ctx.Entry(program).Property(u => u.Description).IsModified = true;
            _ctx.Entry(program).Property(u => u.IsActive).IsModified = true;
            _ctx.Entry(program).Property(u => u.UpdatedBy).IsModified = true;
            _ctx.Entry(program).Property(u => u.UpdatedOn).IsModified = true;
        }

        var rowsAffected = await _ctx.SaveChangesAsync();
        DetachEntity(program);
        return rowsAffected > 0;
    }


    public async Task<bool> ProgramExistsByNameAsync(string name, long programId = 0)
    {
        return await _ctx.Programs
            .AsNoTracking()
            .AnyAsync(x =>
                x.Name.ToLower() == name.ToLower() &&
                (programId == 0 || x.Id != programId)
            );
    }

    public async Task<IEnumerable<Models.Program>?> GetAllProgramsForDropdownAsync(long programId = 0)
    {
        return await _ctx.Programs
            .AsNoTracking()
            .Where(x => (x.IsActive || (programId > 0 && x.Id == programId)))
            .Select(x => new Models.Program
            {
                Id = x.Id,
                Name = x.Name
            })
            .ToListAsync();
    }

    #endregion

    #region # User

    public async Task<PaginatedList<User>> GetAllUsersAsync(int currentPage, int pageSize, string? searchTerm = null)
    {
        var SP = DBConstant.SP.usp_GetAllUsers;
        var P = new DynamicParameters();

        P.Add("@CurrentPage", currentPage);
        P.Add("@PageSize", pageSize);
        P.Add("@SearchTerm", searchTerm);

        var dapperResp = await _ctx.Connection
            .QueryMultipleAsync(SP, P, null, null, commandType: CommandType.StoredProcedure);

        var records = (await dapperResp.ReadAsync<User>()).ToList();
        var totalRecords = await dapperResp.ReadSingleAsync<int>();

        return new PaginatedList<User>
        {
            PageSize = pageSize,
            CurrentPage = currentPage,
            TotalPages = (int)Math.Ceiling(totalRecords / (double)pageSize),
            TotalRecords = totalRecords,
            Records = records
        };
    }

    public async Task<User?> GetUserByIdAsync(long userId, bool track = false)
    {
        var query = _ctx.Users.AsQueryable();

        if (track is false)
        {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync(x => x.Id == userId);
    }

    public async Task<bool> UpsertUserAsync(User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        var loggedInUserId = await _authService.GetLoggedInUserIdAsync();
        var currentTime = DateTime.UtcNow;

        if (user.Id <= 0)
        {
            user.CreatedBy = loggedInUserId;
            user.CreatedOn = currentTime;

            await _ctx.Users.AddAsync(user);
        }
        else
        {
            user.UpdatedBy = loggedInUserId;
            user.UpdatedOn = currentTime;

            _ctx.Users.Attach(user);

            _ctx.Entry(user).Property(u => u.FirstName).IsModified = true;
            _ctx.Entry(user).Property(u => u.LastName).IsModified = true;
            _ctx.Entry(user).Property(u => u.EmailId).IsModified = true;
            _ctx.Entry(user).Property(u => u.MobileNo).IsModified = true;
            _ctx.Entry(user).Property(u => u.Password).IsModified = true;
            _ctx.Entry(user).Property(u => u.Role).IsModified = true;
            _ctx.Entry(user).Property(u => u.IsActive).IsModified = true;

            _ctx.Entry(user).Property(u => u.UpdatedBy).IsModified = true;
            _ctx.Entry(user).Property(u => u.UpdatedOn).IsModified = true;
        }

        var rowsAffected = await _ctx.SaveChangesAsync();
        DetachEntity(user);
        return rowsAffected > 0;
    }


    public async Task<bool> UserExistsByEmailAsync(string emailId, long userId = 0)
    {
        return await _ctx.Users
            .AsNoTracking()
            .AnyAsync(x =>
                x.EmailId.ToLower() == emailId.ToLower() &&
                (userId == 0 || x.Id != userId)
            );
    }

    public async Task<User?> FindUserByEmailIdAsync(string emailId)
    {
        return await _ctx.Users
           .AsNoTracking()
           .FirstOrDefaultAsync(x => x.EmailId.ToLower() == emailId.ToLower());
    }

    #endregion

    #region # Family

    public async Task<PaginatedList<FamilyListForAdminDTO>> GetAllFamiliesForAdminAsync(long campaignId, Status status, int currentPage, int pageSize, string? searchTerm = null)
    {
        var SP = DBConstant.SP.usp_GetAllFamiliesForAdmin;
        var P = new DynamicParameters();

        P.Add("@CampaignId", campaignId);
        P.Add("@Status", status);

        P.Add("@CurrentPage", currentPage);
        P.Add("@PageSize", pageSize);
        P.Add("@SearchTerm", searchTerm);

        var dapperResp = await _ctx.Connection
            .QueryMultipleAsync(SP, P, null, null, commandType: CommandType.StoredProcedure);

        var records = (await dapperResp.ReadAsync<FamilyListForAdminDTO>()).ToList();
        var totalRecords = await dapperResp.ReadSingleAsync<int>();

        // Decrypt the Data
        foreach (var record in records)
        {
            //record.DonorFirstName = record.DonorFirstName.Decrypt();
            //record.DonorLastName = record.DonorLastName.Decrypt();
            record.DonorEmailId = record.DonorEmailId.Decrypt();
            record.DonorMobileNo = record.DonorMobileNo.Decrypt();
        }

        return new PaginatedList<FamilyListForAdminDTO>
        {
            PageSize = pageSize,
            CurrentPage = currentPage,
            TotalPages = (int)Math.Ceiling(totalRecords / (double)pageSize),
            TotalRecords = totalRecords,
            Records = records
        };
    }

    public async Task<PaginatedList<FamilyListForAdvocateDTO>> GetAllFamiliesForAdvocateAsync(long campaignId, Status status, int currentPage, int pageSize, string? searchTerm = null)
    {
        //var loggedInUserId = await _authService.GetLoggedInUserIdAsync();

        var SP = DBConstant.SP.usp_GetAllFamiliesForAdvocate;
        var P = new DynamicParameters();

        P.Add("@CampaignId", campaignId);
        P.Add("@Status", status);
        //P.Add("@UserId", loggedInUserId);

        P.Add("@CurrentPage", currentPage);
        P.Add("@PageSize", pageSize);
        P.Add("@SearchTerm", searchTerm);

        var dapperResp = await _ctx.Connection
            .QueryMultipleAsync(SP, P, null, null, commandType: CommandType.StoredProcedure);

        var records = (await dapperResp.ReadAsync<FamilyListForAdvocateDTO>()).ToList();
        var totalRecords = await dapperResp.ReadSingleAsync<int>();

        return new PaginatedList<FamilyListForAdvocateDTO>
        {
            PageSize = pageSize,
            CurrentPage = currentPage,
            TotalPages = (int)Math.Ceiling(totalRecords / (double)pageSize),
            TotalRecords = totalRecords,
            Records = records
        };
    }

    public async Task<List<FamilyWithChildrenForDonorDTO>?> GetAllFamiliesForDonorAsync(long campaignId)
    {
        var families = await _ctx.Families
            .AsNoTracking()
            .Where(f => f.CampaignId == campaignId && f.Status == Status.Registered && f.IsActive)
            .OrderBy(f => Guid.NewGuid())   // Randomize at the database level
            .Take(100)                      // Limit the number of records fetched
            .GroupJoin(
                _ctx.FamilyMembers,
                family => family.Id,
                member => member.FamilyId,
                (family, members) => new FamilyWithChildrenForDonorDTO
                {
                    Id = family.Id,
                    MemberCount = members.Count()
                }
            )
            .Where(f => f.MemberCount > 0)
            .ToListAsync();

        families = families.OrderBy(x => x.MemberCount).ToList();

        return families;
    }

    public async Task<Family?> GetFamilyWithDetailsForAdminByFamilyIdAsync(long familyId, bool track = false)
    {
        // TODO: Use DTO & SP instead
        var query = _ctx.Families.AsQueryable();

        if (track is false)
        {
            query = query.AsNoTracking();
        }

        var familyDetails = await query
            .Include(f => f.Campaign)
            .Include(f => f.Program)
            .Include(f => f.Donor)
            .Include(f => f.FamilyMembers.Where(x => x.IsActive))
            .FirstOrDefaultAsync(x => x.Id == familyId);

        return familyDetails;
    }

    public async Task<Family?> GetFamilyWithDetailsForAdvocateByFamilyIdAsync(long familyId, bool track = false)
    {
        // TODO: Use DTO & SP instead
        //var loggedInUserId = await _authService.GetLoggedInUserIdAsync();

        var query = _ctx.Families.AsQueryable();

        if (track is false)
        {
            query = query.AsNoTracking();
        }

        var familyDetails = await query
            .Include(f => f.Campaign)
            .Include(f => f.Program)
            .Include(f => f.FamilyMembers.Where(x => x.IsActive))
            .FirstOrDefaultAsync(x => x.Id == familyId /*&& x.CreatedBy == loggedInUserId*/);

        return familyDetails;
    }

    public async Task<Family?> GetFamilyWithDetailsForDonorByFamilyIdAsync(long familyId, bool track = false)
    {
        // TODO: Use DTO & SP instead
        var query = _ctx.Families.AsQueryable();

        if (track is false)
        {
            query = query.AsNoTracking();
        }

        var familyDetails = await query
            .Include(f => f.Campaign)
            .Include(f => f.Program)
            .Include(f => f.FamilyMembers.Where(x => x.IsActive))
            .FirstOrDefaultAsync(x => x.Id == familyId && x.IsActive == true);

        return familyDetails;
    }

    public async Task<List<long>> GetAlreadyAdoptedFamilyIdsAsync(List<long> familyIds)
    {
        var alreadyAdoptedFamilyIds = await _ctx.Families
            .AsNoTracking()
            .Where(f => familyIds.Contains(f.Id) && f.Status >= Status.Adopted)
            .Select(f => f.Id)
            .ToListAsync();

        return alreadyAdoptedFamilyIds;
    }

    public async Task<Family?> GetFamilyByIdAsync(long familyId, bool track = false)
    {
        var query = _ctx.Families.AsQueryable();

        if (track is false)
        {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync(x => x.Id == familyId);
    }

    public async Task<bool> UpsertFamilyAsync(Family family)
    {
        ArgumentNullException.ThrowIfNull(family);

        var loggedInUserId = await _authService.GetLoggedInUserIdAsync();
        var currentTime = DateTime.UtcNow;

        if (family.Id <= 0)
        {
            family.Status = Status.InProgress;
            family.CreatedBy = loggedInUserId;
            family.CreatedOn = currentTime;

            await _ctx.Families.AddAsync(family);
        }
        else
        {
            family.UpdatedBy = loggedInUserId;
            family.UpdatedOn = currentTime;

            _ctx.Families.Attach(family);

            _ctx.Entry(family).Property(u => u.ClientName).IsModified = true;
            _ctx.Entry(family).Property(u => u.ClientCode).IsModified = true;
            _ctx.Entry(family).Property(u => u.SalesForceNumber).IsModified = true;
            _ctx.Entry(family).Property(u => u.ProgramId).IsModified = true;
            _ctx.Entry(family).Property(u => u.NeedMealKit).IsModified = true;
            _ctx.Entry(family).Property(u => u.MealKitNotes).IsModified = true;
            _ctx.Entry(family).Property(u => u.UpdatedBy).IsModified = true;
            _ctx.Entry(family).Property(u => u.UpdatedOn).IsModified = true;
        }

        var rowsAffected = await _ctx.SaveChangesAsync();
        DetachEntity(family);
        return rowsAffected > 0;
    }

    public async Task<bool> UpdateFamilyRegistrationStatusAsync(long familyId, Status status)
    {
        if (familyId <= 0)
        {
            throw new ArgumentException("Invalid familyId.", nameof(familyId));
        }
        if (status == Status.None)
        {
            throw new ArgumentException("Invalid status.", nameof(status));
        }

        var loggedInUserId = await _authService.GetLoggedInUserIdAsync();
        var currentTime = DateTime.UtcNow;

        var family = new Family
        {
            Id = familyId,
            DonorId = null, // Only if reverting back to {Registered}
            Status = status,
            UpdatedBy = loggedInUserId,
            UpdatedOn = currentTime
        };

        _ctx.Families.Attach(family);

        if (status == Status.Registered)
        {
            _ctx.Entry(family).Property(u => u.DonorId).IsModified = true;
        }
        _ctx.Entry(family).Property(u => u.Status).IsModified = true;
        _ctx.Entry(family).Property(u => u.UpdatedBy).IsModified = true;
        _ctx.Entry(family).Property(u => u.UpdatedOn).IsModified = true;

        var rowsAffected = await _ctx.SaveChangesAsync();
        DetachEntity(family);
        return rowsAffected > 0;
    }


    public async Task<bool> FamilyExistsByClientCodeAsync(string clientCode, long familyId = 0)
    {
        return await _ctx.Families
            .AsNoTracking()
            .AnyAsync(x =>
                x.ClientCode.ToLower() == clientCode.ToLower() &&
                (familyId == 0 || x.Id != familyId)
            );
    }

    public async Task<bool> FamilyExistsBySalesForcenumberAsync(string salesForceNumber, long familyId = 0)
    {
        return await _ctx.Families
            .AsNoTracking()
            .AnyAsync(x =>
                x.SalesForceNumber.ToLower() == salesForceNumber.ToLower() &&
                (familyId == 0 || x.Id != familyId)
            );
    }

    public async Task<List<Family>> GetAllFamilyDataForExportAsync(long campaignId, Status status, long? userId = null)
    {
        var query = _ctx.Families
         .AsNoTracking()
         .Include(x => x.Campaign)
         .Include(x => x.Program)
         .Include(x => x.FamilyMembers.Where(x => x.IsActive))
         .Include(x => x.Donor)
         .Where(x => x.CampaignId == campaignId && x.Status == status && x.IsActive);

        if (userId.HasValue)
        {
            query = query.Where(x => x.CreatedBy == userId.Value);
        }

        return await query.OrderByDescending(x => x.Id).ToListAsync();
    }

    public async Task<bool> DeleteFamilyForAdminByIdAsync(long familyId)
    {
        if (familyId <= 0)
        {
            throw new ArgumentException("Invalid userId.", nameof(familyId));
        }

        var loggedInUserId = await _authService.GetLoggedInUserIdAsync();
        var currentTime = DateTime.UtcNow;

        var family = new Family
        {
            Id = familyId,
            IsActive = false,

            UpdatedBy = loggedInUserId,
            UpdatedOn = currentTime
        };

        _ctx.Families.Attach(family);

        _ctx.Entry(family).Property(u => u.IsActive).IsModified = true;
        _ctx.Entry(family).Property(u => u.UpdatedBy).IsModified = true;
        _ctx.Entry(family).Property(u => u.UpdatedOn).IsModified = true;

        var rowsAffected = await _ctx.SaveChangesAsync();
        DetachEntity(family);
        return rowsAffected > 0;
    }

    #endregion

    #region # Family Members

    public async Task<IEnumerable<FamilyMember>> GetAllFamilyMembersByFamilyIdAsync(long familyId)
    {
        return await _ctx.FamilyMembers
            .AsNoTracking()
            .Where(x => x.FamilyId == familyId && x.IsActive == true)
            .Select(x => new FamilyMember
            {
                Id = x.Id,
                FamilyId = x.FamilyId,
                Age = x.Age,
                Gender = x.Gender,
                FavoriteColor = x.FavoriteColor,
                ClothSize = x.ClothSize,
                ShoeSize = x.ShoeSize,
                Notes = x.Notes,
                IsActive = x.IsActive
            })
            .ToListAsync();
    }

    public async Task<bool> UpsertFamilyMembersAsync(long familyId, List<FamilyMember> familyMembers)
    {
        var loggedInUserId = await _authService.GetLoggedInUserIdAsync();

        // Fetch existing family members for the given familyId
        var existingFamilyMembers = await _ctx.FamilyMembers
            .Where(x => x.FamilyId == familyId)
            .ToListAsync();

        // Determine members to add, update, and delete
        var membersToAdd = familyMembers.Where(x => x.Id <= 0).ToList();
        var membersToUpdate = familyMembers.Where(x => x.Id > 0).ToList();
        var membersToDelete = existingFamilyMembers
            .Where(efm => !familyMembers.Any(fm => fm.Id == efm.Id))
            .ToList();

        /* ---------------
         * Special Case
         * ---------------
         * If the page opens in edit more & there is only one child entry 
         * Now if the user deleted that only child
         * In that case we cant disable the Save & next button,
         * Hence handling the scenario here
        */
        if (membersToAdd.Count == 0 && membersToUpdate.Count == 0 && membersToDelete.Count == 0)
        {
            return true;
        }

        // Add new members
        foreach (var member in membersToAdd)
        {
            member.FamilyId = familyId;

            member.CreatedBy = loggedInUserId;
            member.CreatedOn = DateTime.UtcNow;

            await _ctx.FamilyMembers.AddAsync(member);
        }

        // Update existing members
        foreach (var member in membersToUpdate)
        {
            var existingMember = existingFamilyMembers.FirstOrDefault(x => x.Id == member.Id);
            if (existingMember != null)
            {
                existingMember.Age = member.Age;
                existingMember.Gender = member.Gender;
                existingMember.FavoriteColor = member.FavoriteColor;
                existingMember.ClothSize = member.ClothSize;
                existingMember.ShoeSize = member.ShoeSize;
                existingMember.Notes = member.Notes;

                existingMember.UpdatedBy = loggedInUserId;
                existingMember.UpdatedOn = DateTime.UtcNow;

                _ctx.FamilyMembers.Update(existingMember);
            }
        }

        // Delete members not in the provided list
        _ctx.FamilyMembers.RemoveRange(membersToDelete);

        // Save changes to the database
        var rowsAffected = await _ctx.SaveChangesAsync();

        // Detach the entities to stop tracking
        foreach (var member in familyMembers)
        {
            _ctx.Entry(member).State = EntityState.Detached;
        }
        foreach (var member in membersToDelete)
        {
            _ctx.Entry(member).State = EntityState.Detached;
        }

        return rowsAffected > 0;
    }

    #endregion

    #region # Donor

    public async Task<bool> UpsertDonorAsync(Donor donor)
    {
        ArgumentNullException.ThrowIfNull(donor);

        var currentTime = DateTime.UtcNow;

        if (donor.Id <= 0)
        {
            donor.CreatedBy = -1;
            donor.CreatedOn = currentTime;

            await _ctx.Donors.AddAsync(donor);
        }
        else
        {
            var loggedInUserId = await _authService.GetLoggedInUserIdAsync();

            donor.UpdatedBy = loggedInUserId;
            donor.UpdatedOn = currentTime;

            _ctx.Donors.Attach(donor);

            _ctx.Entry(donor).Property(u => u.EmailId).IsModified = true;
            _ctx.Entry(donor).Property(u => u.UpdatedBy).IsModified = true;
            _ctx.Entry(donor).Property(u => u.UpdatedOn).IsModified = true;
        }

        var rowsAffected = await _ctx.SaveChangesAsync();
        DetachEntity(donor);
        return rowsAffected > 0;
    }

    public async Task<bool> MarkFamiliesAsAdopted(long donorId, List<long> selectedFamilyIds)
    {
        var familiesToDetach = new List<Family>();
        foreach (var familyId in selectedFamilyIds)
        {
            var family = new Family
            {
                Id = familyId,
                DonorId = donorId,
                Status = Status.Adopted,
                UpdatedBy = donorId,
                UpdatedOn = DateTime.UtcNow
            };

            _ctx.Families.Attach(family);

            _ctx.Entry(family).Property(f => f.Status).IsModified = true;
            _ctx.Entry(family).Property(f => f.DonorId).IsModified = true;
            _ctx.Entry(family).Property(f => f.UpdatedBy).IsModified = true;
            _ctx.Entry(family).Property(f => f.UpdatedOn).IsModified = true;

            familiesToDetach.Add(family);
        }

        var rowsaffected = await _ctx.SaveChangesAsync();
        foreach (var familyToDetach in familiesToDetach)
        {
            DetachEntity(familyToDetach);
        }
        return rowsaffected > 0;
    }

    public async Task<Donor?> GetDonorByFamilyIdAsync(long familyId)
    {
        var family = await _ctx.Families
            .AsNoTracking()
            .Include(f => f.Donor)
            .FirstOrDefaultAsync(f => f.Id == familyId);

        return family?.Donor;
    }

    #endregion

    #region # Dashboard

    public async Task<DashboardDetailsDTO?> GetDashboardDetailsAsync(long campaignId)
    {
        if (campaignId <= 0)
        {
            throw new ArgumentException("Invalid campaignId.", nameof(campaignId));
        }

        var loggedInUserId = await _authService.GetLoggedInUserIdAsync();
        var stats = new DashboardDetailsDTO();

        var records = await _ctx.Families
            .AsNoTracking()
            .Where(x => x.CampaignId == campaignId && x.IsActive == true)
            .Select(y => new
            {
                Id = y.Id,
                Status = y.Status,
                CreatedBy = y.CreatedBy
            })
            .ToListAsync();

        // All Stats
        stats.TotalRegistered = records.Where(x => x.Status >= Status.Registered).Count();
        stats.TotalAdopted = records.Where(x => x.Status >= Status.Adopted).Count();
        stats.TotalReceived = records.Where(x => x.Status >= Status.Recieved).Count();
        stats.TotalDistributed = records.Where(x => x.Status == Status.Distributed).Count();

        // For Advocate
        stats.TotalRegisteredByAdvocate = records.Where(x => x.CreatedBy == loggedInUserId && x.Status >= Status.Registered).Count();
        stats.TotalAdoptedByAdvocate = records.Where(x => x.CreatedBy == loggedInUserId && x.Status >= Status.Adopted).Count();
        stats.TotalReceivedByAdvocate = records.Where(x => x.CreatedBy == loggedInUserId && x.Status >= Status.Recieved).Count();
        stats.TotalDistributedByAdvocate = records.Where(x => x.CreatedBy == loggedInUserId && x.Status == Status.Distributed).Count();

        return stats;
    }

    #endregion

    #region # Late Registration

    public async Task<PaginatedList<LateRegistration>> GetAllLateRegistrationsForAdvocateAsync(long campaignId, int currentPage, int pageSize, string? searchTerm = null)
    {
        var loggedInUserId = await _authService.GetLoggedInUserIdAsync();

        var SP = DBConstant.SP.usp_GetAllLateRegistrationsForAdvocate;
        var P = new DynamicParameters();

        P.Add("@UserId", loggedInUserId);
        P.Add("@CampaignId", campaignId);

        P.Add("@CurrentPage", currentPage);
        P.Add("@PageSize", pageSize);
        P.Add("@SearchTerm", searchTerm);

        var dapperResp = await _ctx.Connection
            .QueryMultipleAsync(SP, P, null, null, commandType: CommandType.StoredProcedure);

        var records = (await dapperResp.ReadAsync<LateRegistration>()).ToList();
        var totalRecords = await dapperResp.ReadSingleAsync<int>();

        return new PaginatedList<LateRegistration>
        {
            PageSize = pageSize,
            CurrentPage = currentPage,
            TotalPages = (int)Math.Ceiling(totalRecords / (double)pageSize),
            TotalRecords = totalRecords,
            Records = records
        };
    }

    public async Task<PaginatedList<LateRegistration>> GetAllLateRegistrationsForAdminAsync(long campaignId, int currentPage, int pageSize, string? searchTerm = null)
    {
        var SP = DBConstant.SP.usp_GetAllLateRegistrationsForAdmin;
        var P = new DynamicParameters();

        P.Add("@CampaignId", campaignId);

        P.Add("@CurrentPage", currentPage);
        P.Add("@PageSize", pageSize);
        P.Add("@SearchTerm", searchTerm);

        var dapperResp = await _ctx.Connection
            .QueryMultipleAsync(SP, P, null, null, commandType: CommandType.StoredProcedure);

        var records = (await dapperResp.ReadAsync<LateRegistration>()).ToList();
        var totalRecords = await dapperResp.ReadSingleAsync<int>();

        return new PaginatedList<LateRegistration>
        {
            PageSize = pageSize,
            CurrentPage = currentPage,
            TotalPages = (int)Math.Ceiling(totalRecords / (double)pageSize),
            TotalRecords = totalRecords,
            Records = records
        };
    }

    public async Task<LateRegistration?> GetLateRegistrationByIdAsync(long registrationId, bool track = false)
    {
        var query = _ctx.LateRegistrations.AsQueryable();

        if (track is false)
        {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync(x => x.Id == registrationId);
    }

    public async Task<bool> UpsertLateRegistrationAsync(LateRegistration lateRegistration)
    {
        ArgumentNullException.ThrowIfNull(lateRegistration);

        var loggedInUserId = await _authService.GetLoggedInUserIdAsync();
        var currentTime = DateTime.UtcNow;

        if (lateRegistration.Id <= 0)
        {
            lateRegistration.CreatedBy = loggedInUserId;
            lateRegistration.CreatedOn = currentTime;

            await _ctx.LateRegistrations.AddAsync(lateRegistration);
        }
        else
        {
            lateRegistration.UpdatedBy = loggedInUserId;
            lateRegistration.UpdatedOn = currentTime;

            _ctx.LateRegistrations.Attach(lateRegistration);

            _ctx.Entry(lateRegistration).Property(u => u.ClientName).IsModified = true;
            _ctx.Entry(lateRegistration).Property(u => u.NoOfMaleChildren).IsModified = true;
            _ctx.Entry(lateRegistration).Property(u => u.NoOfFemaleChildren).IsModified = true;
            _ctx.Entry(lateRegistration).Property(u => u.IsActive).IsModified = true;
            _ctx.Entry(lateRegistration).Property(u => u.UpdatedBy).IsModified = true;
            _ctx.Entry(lateRegistration).Property(u => u.UpdatedOn).IsModified = true;
        }

        var rowsAffected = await _ctx.SaveChangesAsync();
        DetachEntity(lateRegistration);
        return rowsAffected > 0;
    }

    public async Task<List<LateRegistration>> GetAllLateRegistrationDataForExportAsync(long campaignId)
    {
        var query = _ctx.LateRegistrations
         .AsNoTracking()
         .Where(x => x.CampaignId == campaignId && x.IsActive == true);

        return await query.OrderByDescending(x => x.Id).ToListAsync();
    }

    public async Task<List<LateRegistration>> GetAllLateRegistrationDataForArchiveExportAsync(long campaignId)
    {
        var query = _ctx.LateRegistrations
         .AsNoTracking()
         .Where(x => x.CampaignId == campaignId);

        return await query.OrderByDescending(x => x.Id).ToListAsync();
    }

    #endregion

    #region # Helper Methods

    private void DetachEntity(object entity)
    {
        var entry = _ctx.Entry(entity);
        if (entry != null)
        {
            entry.State = EntityState.Detached;
            foreach (var navigation in entry.Navigations)
            {
                if (navigation.CurrentValue != null)
                {
                    if (navigation.CurrentValue is IEnumerable<object> collection)
                    {
                        foreach (var relatedEntity in collection)
                        {
                            DetachEntity(relatedEntity);
                        }
                    }
                    else
                    {
                        DetachEntity(navigation.CurrentValue);
                    }
                }
            }
        }
    }


    private async Task<bool> DecryptData()
    {
        //var donors = await _ctx.Donors.AsNoTracking().ToListAsync();

        //foreach (var donor in donors)
        //{
        //donor.FirstName = donor.FirstName.Decrypt();
        //donor.LastName = donor.LastName.Decrypt();
        //_ctx.Donors.Attach(donor);

        //_ctx.Entry(donor).Property(d => d.FirstName).IsModified = true;
        //_ctx.Entry(donor).Property(d => d.LastName).IsModified = true;
        //}

        //var rowsAffected = await _ctx.SaveChangesAsync();
        //return rowsAffected > 0;

        return true;
    }

    #endregion
}
