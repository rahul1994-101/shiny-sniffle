using DocumentFormat.OpenXml.Wordprocessing;

using System.ComponentModel.DataAnnotations;

namespace AAF.Models;

public enum ErrorCode
{
    BadRequest = 400,
    NotFound = 404,
    InternalServerError = 500,

    //Unauthorized = 401,
    //Forbidden = 403,

    UnknownError = 0
}

public enum Roles
{
    [Display(Name = "None")]
    None = 0,
    [Display(Name = "Admin")]
    Admin = 1,
    [Display(Name = "Advocate")]
    Advocate = 2
}

public enum Gender
{
    [Display(Name = "None")]
    None = 0,
    [Display(Name = "Girl")]
    Girl = 1,
    [Display(Name = "Boy")]
    Boy = 2,
    [Display(Name = "Non-Binary")]
    NonBinary = 3
}

public enum Status
{
    [Display(Name = "None")]
    None = 0,
    [Display(Name = "In Progress")]
    InProgress = 1,
    [Display(Name = "Registered")]
    Registered = 2,
    [Display(Name = "Adopted")]
    Adopted = 3,
    [Display(Name = "Recieved")]
    Recieved = 4,
    [Display(Name = "Distributed")]
    Distributed = 5
}

public enum ViewFamilyDetailsAs
{
    Admin = 0,
    Advocate = 1,
    Donor = 2
}

public enum DataFilter
{
    [Display(Name = "None")]
    None = 0,
    [Display(Name = "Adoption Needed")]
    AdoptionNeeded = 1,
    [Display(Name = "Adopted")]
    Adopted = 2,
    [Display(Name = "Recieved")]
    Recieved = 3,
    [Display(Name = "Distributed")]
    Distributed = 4,
    [Display(Name = "Late Registration")]
    LateRegistration = 5
}
