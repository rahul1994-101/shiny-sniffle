using AAF.Utilities;

using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;

namespace AAF.Models;



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

