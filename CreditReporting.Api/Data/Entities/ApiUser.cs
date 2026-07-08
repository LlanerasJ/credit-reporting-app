namespace CreditReporting.Api.Data.Entities;

/// <summary>Staff/lender login for the demo. Passwords are stored as PBKDF2 hashes.</summary>
public class ApiUser
{
    public int Id { get; set; }
    public string Username { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string Role { get; set; } = "Analyst";
    public string DisplayName { get; set; } = "";
}
