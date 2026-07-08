namespace CreditReporting.Shared.Dtos;

/// <summary>Row returned by customer search. SSN is always masked.</summary>
public class CustomerSummaryDto
{
    public int Id { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public DateTime DateOfBirth { get; set; }
    /// <summary>Masked, e.g. "***-**-1234".</summary>
    public string SsnMasked { get; set; } = "";
    public string City { get; set; } = "";
    public string State { get; set; } = "";
    public int OpenAccountCount { get; set; }

    public string FullName => $"{FirstName} {LastName}";
}

public class CustomerDetailDto
{
    public int Id { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public DateTime DateOfBirth { get; set; }
    public string SsnMasked { get; set; } = "";
    public string AddressLine1 { get; set; } = "";
    public string City { get; set; } = "";
    public string State { get; set; } = "";
    public string PostalCode { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Email { get; set; } = "";

    public string FullName => $"{FirstName} {LastName}";
    public string CityStateZip => $"{City}, {State} {PostalCode}";
}
