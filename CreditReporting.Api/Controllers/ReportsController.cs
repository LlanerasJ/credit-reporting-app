using System.Security.Claims;
using CreditReporting.Api.Reports;
using CreditReporting.Api.Services;
using CreditReporting.Shared.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CreditReporting.Api.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly IReportCatalog _catalog;
    private readonly IAuditService _audit;

    public ReportsController(IReportCatalog catalog, IAuditService audit)
    {
        _catalog = catalog;
        _audit = audit;
    }

    /// <summary>The available report types and their parameter descriptors.</summary>
    [HttpGet("catalog")]
    [ProducesResponseType(typeof(List<ReportDefinitionDto>), StatusCodes.Status200OK)]
    public ActionResult<List<ReportDefinitionDto>> Catalog() =>
        Ok(_catalog.All.Select(d => new ReportDefinitionDto
        {
            Key = d.Key,
            DisplayName = d.DisplayName,
            Description = d.Description,
            Parameters = d.Parameters.ToList()
        }).ToList());

    /// <summary>
    /// Runs a catalog report with the supplied parameters. Each run is recorded
    /// in the audit log with the caller and the parameter values used.
    /// </summary>
    [HttpPost("run")]
    [ProducesResponseType(typeof(ReportResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ReportResultDto>> Run(RunReportRequest request, CancellationToken ct)
    {
        var definition = _catalog.Find(request.ReportType);
        if (definition is null)
            return BadRequest(new ProblemDetails { Title = $"Unknown report type '{request.ReportType}'." });

        var args = ReportArgs.Bind(definition.Parameters, request.Parameters, out var errors);
        if (args is null)
            return BadRequest(new ProblemDetails { Title = string.Join(" ", errors) });

        var result = await definition.ExecuteAsync(args, ct);

        // CustomerId 0 = not tied to a single customer; reports span the portfolio.
        await _audit.LogAsync(
            User.FindFirstValue(ClaimTypes.Name) ?? "unknown",
            customerId: 0,
            action: "ReportRun",
            purpose: $"{definition.Key}: {args.Describe()}",
            HttpContext.Connection.RemoteIpAddress?.ToString() ?? "",
            ct);

        return Ok(result);
    }
}
