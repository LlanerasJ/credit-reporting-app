using CreditReporting.Api.Data.Entities;
using CreditReporting.Api.Services;

namespace CreditReporting.Api.Data;

/// <summary>
/// Seeds a deterministic synthetic dataset: 15 fake customers with multiple
/// accounts, 24 months of payment history each, inquiries, and score history.
/// All SSNs use the 900-xx-xxxx range, which is never issued to real people.
/// </summary>
public static class DbSeeder
{
    private static readonly string[] FirstNames =
        { "Ava", "Liam", "Maya", "Noah", "Zoe", "Ethan", "Ruby", "Owen", "Iris", "Felix", "Nora", "Jasper", "Lena", "Miles", "Cora" };
    private static readonly string[] LastNames =
        { "Testman", "Sampleton", "Mockwell", "Faux", "Demoson", "Placeholder", "Stubbs", "Dummy", "Synthetica", "Fakerly", "Modelo", "Specimen", "Protopoulos", "Simulà", "Exemplar" };
    private static readonly string[] Streets =
        { "101 Elm St", "202 Oak Ave", "303 Pine Rd", "404 Maple Dr", "505 Cedar Ln", "606 Birch Blvd", "707 Walnut Way", "808 Aspen Ct" };
    private static readonly (string City, string State, string Zip)[] Cities =
    {
        ("Springfield", "IL", "62701"), ("Fairview", "OH", "44126"), ("Riverton", "UT", "84065"),
        ("Georgetown", "TX", "78626"), ("Madison", "WI", "53703"), ("Clayton", "MO", "63105"),
        ("Franklin", "TN", "37064"), ("Ashland", "OR", "97520")
    };
    private static readonly Dictionary<AccountType, string[]> CreditorsByType = new()
    {
        [AccountType.CreditCard] = new[] { "Acme Card Services", "First Demo Bank", "Synthetic Credit Union" },
        [AccountType.RetailCard] = new[] { "Example Retail Card" },
        [AccountType.AutoLoan] = new[] { "Sample Auto Finance" },
        [AccountType.Mortgage] = new[] { "Mock Mortgage Co", "First Demo Bank" },
        [AccountType.PersonalLoan] = new[] { "Placeholder Lending", "Synthetic Credit Union" },
        [AccountType.StudentLoan] = new[] { "Demo Student Loan Servicing" },
        [AccountType.LineOfCredit] = new[] { "Synthetic Credit Union", "First Demo Bank" }
    };
    private static readonly string[] InquiryPullers =
        { "First Demo Bank", "Acme Card Services", "Sample Auto Finance", "Mock Mortgage Co", "Demo Employment Screening", "Placeholder Lending" };
    private static readonly string[] Bureaus = { "DemoBureau North", "DemoBureau South", "DemoBureau West" };

    public static void Seed(AppDbContext db)
    {
        if (db.Customers.Any()) return;

        var rng = new Random(20260707); // deterministic seed
        var today = new DateTime(2026, 7, 1);

        // Demo logins
        db.Users.AddRange(
            new ApiUser { Username = "analyst", PasswordHash = Masking.HashPassword("Demo123!"), Role = "Analyst", DisplayName = "Demo Analyst" },
            new ApiUser { Username = "admin", PasswordHash = Masking.HashPassword("Admin123!"), Role = "Admin", DisplayName = "Demo Admin" });

        // Example saved reports so the Reporting tab isn't empty on first run
        db.SavedReports.AddRange(
            new SavedReport
            {
                Name = "High-risk past due (all states)",
                Description = "Delinquent accounts at least $500 past due, company-wide.",
                ReportType = "delinquent-accounts",
                ParametersJson = """{"minPastDue":"500"}""",
                OwnerUsername = "admin",
                IsShared = true,
                CreatedUtc = today,
                ModifiedUtc = today
            },
            new SavedReport
            {
                Name = "Texas delinquencies",
                Description = "Delinquent accounts for customers in TX.",
                ReportType = "delinquent-accounts",
                ParametersJson = """{"state":"TX"}""",
                OwnerUsername = "analyst",
                IsShared = false,
                CreatedUtc = today,
                ModifiedUtc = today
            });

        for (int i = 0; i < 15; i++)
        {
            var city = Cities[rng.Next(Cities.Length)];
            // 900-xx-xxxx: reserved range, never a real SSN
            string rawSsn = $"900{rng.Next(10, 99)}{1000 + i:D4}";

            var customer = new Customer
            {
                FirstName = FirstNames[i],
                LastName = LastNames[i],
                DateOfBirth = new DateTime(1955 + rng.Next(0, 45), rng.Next(1, 13), rng.Next(1, 28)),
                SsnHash = Masking.HashSsn(rawSsn),
                SsnLast4 = rawSsn[^4..],
                AddressLine1 = Streets[rng.Next(Streets.Length)],
                City = city.City,
                State = city.State,
                PostalCode = city.Zip,
                Phone = $"555{rng.Next(1000000, 9999999)}",
                Email = $"{FirstNames[i].ToLower()}.{LastNames[i].ToLower()}@example.com"
            };

            int accountCount = rng.Next(2, 6);
            for (int a = 0; a < accountCount; a++)
                customer.Accounts.Add(BuildAccount(rng, today, i, a));

            // Inquiries: 1-5 in the last 2 years
            int inquiryCount = rng.Next(1, 6);
            for (int q = 0; q < inquiryCount; q++)
            {
                customer.Inquiries.Add(new CreditInquiry
                {
                    PulledBy = InquiryPullers[rng.Next(InquiryPullers.Length)],
                    PulledDate = today.AddDays(-rng.Next(1, 730)),
                    InquiryType = rng.NextDouble() < 0.6 ? "Hard" : "Soft",
                    Purpose = rng.NextDouble() < 0.5 ? "Credit application" : "Account review"
                });
            }

            // Quarterly score history over 2 years; drift around a base score
            int baseScore = 540 + rng.Next(0, 300);
            for (int s = 7; s >= 0; s--)
            {
                customer.Scores.Add(new CreditScore
                {
                    Score = Math.Clamp(baseScore + rng.Next(-25, 26), 300, 850),
                    ScoreDate = today.AddMonths(-3 * s),
                    Bureau = Bureaus[rng.Next(Bureaus.Length)],
                    ModelVersion = "DemoScore 3.0"
                });
            }

            db.Customers.Add(customer);
        }

        db.SaveChanges();
    }

    private static Account BuildAccount(Random rng, DateTime today, int customerIndex, int accountIndex)
    {
        var type = (AccountType)rng.Next(0, 7);

        // Portfolio code, size, term (0 = revolving), and APR by product type.
        // For installment products CreditLimit doubles as the original principal.
        (string portfolio, decimal limit, int termMonths, double annualRate) = type switch
        {
            AccountType.CreditCard   => ("R", 500m * rng.Next(2, 40), 0, 0.24),
            AccountType.RetailCard   => ("R", 500m * rng.Next(1, 10), 0, 0.27),
            AccountType.LineOfCredit => ("C", 1000m * rng.Next(5, 50), 0, 0.12),
            AccountType.AutoLoan     => ("I", 1000m * rng.Next(8, 45), 60, 0.07),
            AccountType.PersonalLoan => ("I", 1000m * rng.Next(2, 25), 48, 0.11),
            AccountType.StudentLoan  => ("I", 1000m * rng.Next(5, 80), 120, 0.05),
            AccountType.Mortgage     => ("M", 10000m * rng.Next(12, 60), 360, 0.06),
            _ => ("O", 5000m, 0, 0.10)
        };

        // ~15% of accounts are troubled, ~20% closed in good standing
        double fate = rng.NextDouble();
        var status = fate < 0.08 ? AccountStatus.ChargeOff
                   : fate < 0.15 ? AccountStatus.Collection
                   : fate < 0.35 ? AccountStatus.PaidClosed
                   : AccountStatus.Open;

        // An installment loan still being paid can't be older than its term.
        int maxAgeMonths = termMonths > 0 && status != AccountStatus.PaidClosed
            ? Math.Min(178, termMonths - 2)
            : 178;
        int ageMonths = rng.Next(12, Math.Max(14, maxAgeMonths));

        string[] creditors = CreditorsByType[type];
        var account = new Account
        {
            AccountNumber = $"4{customerIndex:D2}{accountIndex}{rng.Next(100000, 999999)}{rng.Next(1000, 9999)}",
            AccountType = type,
            Status = status,
            OpenDate = today.AddMonths(-ageMonths),
            ClosedDate = status is AccountStatus.PaidClosed or AccountStatus.Closed
                ? today.AddMonths(-rng.Next(1, 12)) : null,
            CreditLimit = limit,
            PortfolioType = portfolio,
            EcoaCode = rng.NextDouble() < 0.8 ? "1" : "2",
            CreditorName = creditors[rng.Next(creditors.Length)]
        };

        if (termMonths > 0)
            SimulateInstallment(rng, account, today, ageMonths, termMonths, annualRate);
        else
            SimulateRevolving(rng, account, today, annualRate);

        return account;
    }

    /// <summary>
    /// Amortizes a loan from its original principal. The balance declines each
    /// month and AmountPaid matches the scheduled annuity payment (doubled the
    /// month after a missed payment).
    /// </summary>
    private static void SimulateInstallment(
        Random rng, Account account, DateTime today, int ageMonths, int termMonths, double annualRate)
    {
        bool troubled = account.Status is AccountStatus.ChargeOff or AccountStatus.Collection;
        double r = annualRate / 12.0;
        double principal = (double)account.CreditLimit;
        double scheduled = principal * r / (1 - Math.Pow(1 + r, -termMonths));

        // Remaining balance when the 24-month reporting window opens
        int paymentsBeforeWindow = Math.Max(0, ageMonths - 24);
        double balance = principal
            * (Math.Pow(1 + r, termMonths) - Math.Pow(1 + r, paymentsBeforeWindow))
            / (Math.Pow(1 + r, termMonths) - 1);

        decimal pastDue = 0m;
        int lateStreak = 0;
        bool owesCatchUp = false;
        bool paidOff = false;

        for (int m = 23; m >= 0; m--)
        {
            var date = today.AddMonths(-m);
            if (date < account.OpenDate) continue;

            double interest = balance * r;
            double paid = 0;
            int daysLate = 0;

            if (paidOff)
            {
                // balance is already zero
            }
            else if (account.Status == AccountStatus.PaidClosed &&
                     account.ClosedDate is { } closed && date >= closed)
            {
                paid = balance + interest; // payoff month
                balance = 0;
                paidOff = true;
            }
            else if (troubled && m < 8)
            {
                lateStreak++;
                daysLate = Math.Min(lateStreak * 30, 180);
                pastDue += (decimal)scheduled;
                balance += interest; // no payment, interest keeps accruing
            }
            else if (rng.NextDouble() < 0.08)
            {
                daysLate = 30; // one missed month
                balance += interest;
                owesCatchUp = true;
            }
            else
            {
                paid = owesCatchUp ? scheduled * 2 : scheduled;
                owesCatchUp = false;
                balance = Math.Max(0, balance + interest - paid);
            }

            account.PaymentHistory.Add(new PaymentRecord
            {
                PaymentDate = new DateTime(date.Year, date.Month, 1 + rng.Next(0, 28)),
                Balance = Math.Round((decimal)balance, 2),
                AmountPaid = Math.Round((decimal)paid, 2),
                DaysLate = daysLate,
                PaymentRating = Math.Min(daysLate / 30, 6).ToString()
            });
        }

        account.CurrentBalance = Math.Round((decimal)balance, 2);
        account.AmountPastDue = troubled ? Math.Round(pastDue, 2) : 0m;
    }

    /// <summary>
    /// Simulates a revolving account month by month:
    /// balance = previous + spend + interest - payment, clamped to the limit.
    /// </summary>
    private static void SimulateRevolving(Random rng, Account account, DateTime today, double annualRate)
    {
        bool troubled = account.Status is AccountStatus.ChargeOff or AccountStatus.Collection;
        decimal limit = account.CreditLimit;
        decimal monthlyRate = (decimal)(annualRate / 12.0);
        decimal balance = Math.Round(limit * (decimal)(0.10 + rng.NextDouble() * 0.45), 2);
        decimal pastDue = 0m;
        int lateStreak = 0;
        bool paidOff = false;

        for (int m = 23; m >= 0; m--)
        {
            var date = today.AddMonths(-m);
            if (date < account.OpenDate) continue;

            decimal interest = Math.Round(balance * monthlyRate, 2);
            decimal paid = 0m;
            int daysLate = 0;

            if (paidOff)
            {
                // balance is already zero
            }
            else if (account.Status == AccountStatus.PaidClosed &&
                     account.ClosedDate is { } closed && date >= closed)
            {
                paid = balance + interest; // payoff month
                balance = 0m;
                paidOff = true;
            }
            else if (troubled && m < 8)
            {
                lateStreak++;
                daysLate = Math.Min(lateStreak * 30, 180);
                pastDue += Math.Max(25m, balance * 0.03m);
                balance += interest; // account frozen, no spend or payments
            }
            else
            {
                decimal spend = Math.Round(limit * (decimal)(rng.NextDouble() * 0.12), 2);
                if (rng.NextDouble() < 0.10)
                {
                    daysLate = 30; // missed this month's payment
                }
                else
                {
                    // anywhere from the minimum payment up to a big paydown
                    paid = Math.Round(Math.Max(25m, balance * (decimal)(0.03 + rng.NextDouble() * 0.22)), 2);
                }
                decimal newBalance = balance + spend + interest - paid;
                if (newBalance < 0m)
                {
                    paid = balance + spend + interest; // paid the whole thing off
                    newBalance = 0m;
                }
                balance = Math.Min(newBalance, limit);
            }

            account.PaymentHistory.Add(new PaymentRecord
            {
                PaymentDate = new DateTime(date.Year, date.Month, 1 + rng.Next(0, 28)),
                Balance = Math.Round(balance, 2),
                AmountPaid = Math.Round(paid, 2),
                DaysLate = daysLate,
                PaymentRating = Math.Min(daysLate / 30, 6).ToString()
            });
        }

        account.CurrentBalance = Math.Round(balance, 2);
        account.AmountPastDue = troubled ? Math.Round(pastDue, 2) : 0m;
    }
}
