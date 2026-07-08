using System.Security.Claims;
using CreditReporting.Api.Services;
using CreditReporting.Shared.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CreditReporting.Api.Controllers;

[ApiController]
[Route("api/creditreport")]
[Authorize]
public class CreditReportController : ControllerBase
{
    private readonly ICreditReportService _reports;
    private readonly IAuditService _audit;

    public CreditReportController(ICreditReportService reports, IAuditService audit)
    {
        _reports = reports;
        _audit = audit;
    }

    /// <summary>
    /// Returns the aggregated credit report for a customer. Each access is
    /// recorded in the audit log with the caller and purpose.
    /// </summary>
    [HttpGet("{customerId:int}")]
    [ProducesResponseType(typeof(CreditReportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CreditReportDto>> Get(
        int customerId, [FromQuery] string purpose = "Account review", CancellationToken ct = default)
    {
        var report = await _reports.BuildReportAsync(customerId, ct);
        if (report is null)
            return NotFound(new ProblemDetails { Title = $"Customer {customerId} not found." });

        await _audit.LogAsync(
            User.FindFirstValue(ClaimTypes.Name) ?? "unknown",
            customerId,
            "CreditReportViewed",
            purpose,
            HttpContext.Connection.RemoteIpAddress?.ToString() ?? "",
            ct);

        return Ok(report);
    }
}
