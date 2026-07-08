namespace CreditReporting.Api.Data.Entities;

public class CreditInquiry
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    public string PulledBy { get; set; } = "";
    public DateTime PulledDate { get; set; }
    /// <summary>"Hard" or "Soft".</summary>
    public string InquiryType { get; set; } = "Soft";
    public string Purpose { get; set; } = "";
}
