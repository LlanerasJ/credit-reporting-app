namespace CreditReporting.Api.Data.Entities;

public class CreditScore
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    public int Score { get; set; }
    public DateTime ScoreDate { get; set; }
    public string Bureau { get; set; } = "";
    public string ModelVersion { get; set; } = "";
}
