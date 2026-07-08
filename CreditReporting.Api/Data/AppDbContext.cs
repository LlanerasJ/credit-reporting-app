using CreditReporting.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace CreditReporting.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<PaymentRecord> PaymentHistory => Set<PaymentRecord>();
    public DbSet<CreditInquiry> CreditInquiries => Set<CreditInquiry>();
    public DbSet<CreditScore> CreditScores => Set<CreditScore>();
    public DbSet<AuditLogEntry> AuditLog => Set<AuditLogEntry>();
    public DbSet<ApiUser> Users => Set<ApiUser>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Customer>(e =>
        {
            e.Property(c => c.FirstName).HasMaxLength(50);
            e.Property(c => c.LastName).HasMaxLength(50);
            e.Property(c => c.SsnHash).HasMaxLength(64);
            e.Property(c => c.SsnLast4).HasMaxLength(4);
            e.Property(c => c.State).HasMaxLength(2);
            e.HasIndex(c => c.LastName);
            e.HasIndex(c => c.SsnLast4);
        });

        modelBuilder.Entity<Account>(e =>
        {
            e.Property(a => a.AccountNumber).HasMaxLength(30);
            e.Property(a => a.PortfolioType).HasMaxLength(1);
            e.Property(a => a.EcoaCode).HasMaxLength(1);
            e.Property(a => a.CreditLimit).HasPrecision(18, 2);
            e.Property(a => a.CurrentBalance).HasPrecision(18, 2);
            e.Property(a => a.AmountPastDue).HasPrecision(18, 2);
            e.HasOne(a => a.Customer)
             .WithMany(c => c.Accounts)
             .HasForeignKey(a => a.CustomerId);
        });

        modelBuilder.Entity<PaymentRecord>(e =>
        {
            e.Property(p => p.Balance).HasPrecision(18, 2);
            e.Property(p => p.AmountPaid).HasPrecision(18, 2);
            e.Property(p => p.PaymentRating).HasMaxLength(1);
            e.HasOne(p => p.Account)
             .WithMany(a => a.PaymentHistory)
             .HasForeignKey(p => p.AccountId);
            e.HasIndex(p => new { p.AccountId, p.PaymentDate });
        });

        modelBuilder.Entity<CreditInquiry>(e =>
        {
            e.Property(i => i.InquiryType).HasMaxLength(4);
            e.HasOne(i => i.Customer)
             .WithMany(c => c.Inquiries)
             .HasForeignKey(i => i.CustomerId);
        });

        modelBuilder.Entity<CreditScore>(e =>
        {
            e.HasOne(s => s.Customer)
             .WithMany(c => c.Scores)
             .HasForeignKey(s => s.CustomerId);
        });

        modelBuilder.Entity<ApiUser>(e =>
        {
            e.HasIndex(u => u.Username).IsUnique();
            e.Property(u => u.Username).HasMaxLength(50);
        });

        modelBuilder.Entity<AuditLogEntry>(e =>
        {
            e.HasIndex(a => a.CustomerId);
            e.HasIndex(a => a.TimestampUtc);
        });
    }
}
