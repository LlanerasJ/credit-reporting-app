using CreditReporting.Api.Data;
using CreditReporting.Api.Reports;
using CreditReporting.Api.Reports.Definitions;
using CreditReporting.Api.Services;
using CreditReporting.Shared.Dtos;
using Microsoft.EntityFrameworkCore;

namespace CreditReporting.Tests;

public class SavedReportServiceTests
{
    private static SavedReportService NewService(out AppDbContext db)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        db = new AppDbContext(options);
        var catalog = new ReportCatalog(new IReportDefinition[] { new DelinquentAccountsReport(db) });
        return new SavedReportService(db, catalog);
    }

    private static SaveReportRequest ValidRequest(string name = "TX delinquencies", bool isShared = false) => new()
    {
        Name = name,
        Description = "Test",
        ReportType = "delinquent-accounts",
        Parameters = new() { ["state"] = "TX" },
        IsShared = isShared
    };

    [Fact]
    public async Task Analyst_can_save_private_but_not_shared()
    {
        var service = NewService(out var db);
        using (db)
        {
            var ok = await service.CreateAsync("analyst", isAdmin: false, ValidRequest());
            Assert.Equal(SavedReportStatus.Ok, ok.Status);
            Assert.Equal("analyst", ok.Report!.OwnerUsername);
            Assert.False(ok.Report.IsShared);

            var denied = await service.CreateAsync("analyst", isAdmin: false, ValidRequest(isShared: true));
            Assert.Equal(SavedReportStatus.Invalid, denied.Status);
            Assert.Contains("admins", denied.Error);
        }
    }

    [Fact]
    public async Task Admin_can_share_and_visibility_follows_owner_or_shared()
    {
        var service = NewService(out var db);
        using (db)
        {
            await service.CreateAsync("admin", isAdmin: true, ValidRequest("Org-wide", isShared: true));
            await service.CreateAsync("admin", isAdmin: true, ValidRequest("Admin private"));
            await service.CreateAsync("analyst", isAdmin: false, ValidRequest("Analyst private"));

            var forAnalyst = await service.GetVisibleAsync("analyst");
            Assert.Equal(new[] { "Org-wide", "Analyst private" }, forAnalyst.Select(r => r.Name));

            var forAdmin = await service.GetVisibleAsync("admin");
            Assert.Equal(new[] { "Org-wide", "Admin private" }, forAdmin.Select(r => r.Name));
        }
    }

    [Fact]
    public async Task Only_owner_or_admin_can_update_or_delete()
    {
        var service = NewService(out var db);
        using (db)
        {
            var created = await service.CreateAsync("analyst", isAdmin: false, ValidRequest());
            int id = created.Report!.Id;

            var update = await service.UpdateAsync(id, "someone-else", isAdmin: false, ValidRequest("Hijacked"));
            Assert.Equal(SavedReportStatus.Forbidden, update.Status);

            var delete = await service.DeleteAsync(id, "someone-else", isAdmin: false);
            Assert.Equal(SavedReportStatus.Forbidden, delete.Status);

            var byOwner = await service.UpdateAsync(id, "analyst", isAdmin: false, ValidRequest("Renamed"));
            Assert.Equal(SavedReportStatus.Ok, byOwner.Status);
            Assert.Equal("Renamed", byOwner.Report!.Name);

            var byAdmin = await service.DeleteAsync(id, "admin", isAdmin: true);
            Assert.Equal(SavedReportStatus.Ok, byAdmin.Status);
            Assert.Empty(await service.GetVisibleAsync("analyst"));
        }
    }

    [Fact]
    public async Task Analyst_cannot_flip_own_report_to_shared_via_update()
    {
        var service = NewService(out var db);
        using (db)
        {
            var created = await service.CreateAsync("analyst", isAdmin: false, ValidRequest());
            var flipped = await service.UpdateAsync(
                created.Report!.Id, "analyst", isAdmin: false, ValidRequest(isShared: true));

            Assert.Equal(SavedReportStatus.Invalid, flipped.Status);
            Assert.Contains("admins", flipped.Error);
        }
    }

    [Fact]
    public async Task Save_rejects_unknown_type_bad_parameters_and_missing_name()
    {
        var service = NewService(out var db);
        using (db)
        {
            var badType = ValidRequest();
            badType.ReportType = "nope";
            Assert.Equal(SavedReportStatus.Invalid,
                (await service.CreateAsync("analyst", false, badType)).Status);

            var badParam = ValidRequest();
            badParam.Parameters["minPastDue"] = "abc";
            Assert.Equal(SavedReportStatus.Invalid,
                (await service.CreateAsync("analyst", false, badParam)).Status);

            Assert.Equal(SavedReportStatus.Invalid,
                (await service.CreateAsync("analyst", false, ValidRequest(name: "  "))).Status);

            var missing = await service.UpdateAsync(999, "analyst", false, ValidRequest());
            Assert.Equal(SavedReportStatus.NotFound, missing.Status);
        }
    }
}
