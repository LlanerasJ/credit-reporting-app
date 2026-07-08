namespace CreditReporting.Api.Data.Entities;

public class Customer
{
    public int Id { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public DateTime DateOfBirth { get; set; }

    /// <summary>SHA-256 hash of the SSN. The raw value is never stored.</summary>
    public string SsnHash { get; set; } = "";
    /// <summary>Last 4 digits of the SSN, kept for search and display.</summary>
    public string SsnLast4 { get; set; } = "";

    public string AddressLine1 { get; set; } = "";
    public string City { get; set; } = "";
    public string State { get; set; } = "";
    public string PostalCode { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Email { get; set; } = "";

    public List<Account> Accounts { get; set; } = new();
    public List<CreditInquiry> Inquiries { get; set; } = new();
    public List<CreditScore> Scores { get; set; } = new();
}
