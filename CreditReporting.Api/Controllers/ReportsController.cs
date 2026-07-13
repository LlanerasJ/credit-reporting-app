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
    private readonly ISavedReportService _saved;
    private readonly IAuditService _audit;

    public ReportsController(IReportCatalog catalog, ISavedReportService saved, IAuditService audit)
    {
        _catalog = catalog;
        _saved = saved;
        _audit = audit;
    }

    private string Username => User.FindFirstValue(ClaimTypes.Name) ?? "unknown";
    private bool IsAdmin => User.IsInRole("Admin");

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
            Username,
            customerId: 0,
            action: "ReportRun",
            purpose: $"{definition.Key}: {args.Describe()}",
            HttpContext.Connection.RemoteIpAddress?.ToString() ?? "",
            ct);

        return Ok(result);
    }

    /// <summary>The caller's saved reports plus everything shared.</summary>
    [HttpGet("saved")]
    [ProducesResponseType(typeof(List<SavedReportDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<SavedReportDto>>> GetSaved(CancellationToken ct) =>
        Ok(await _saved.GetVisibleAsync(Username, ct));

    /// <summary>Saves a report configuration. Marking it shared requires the Admin role.</summary>
    [HttpPost("saved")]
    [ProducesResponseType(typeof(SavedReportDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SavedReportDto>> CreateSaved(SaveReportRequest request, CancellationToken ct)
    {
        var result = await _saved.CreateAsync(Username, IsAdmin, request, ct);
        return result.Status == SavedReportStatus.Ok
            ? CreatedAtAction(nameof(GetSaved), result.Report)
            : ToError(result);
    }

    /// <summary>Updates a saved report. Owner or admin only.</summary>
    [HttpPut("saved/{id:int}")]
    [ProducesResponseType(typeof(SavedReportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SavedReportDto>> UpdateSaved(int id, SaveReportRequest request, CancellationToken ct)
    {
        var result = await _saved.UpdateAsync(id, Username, IsAdmin, request, ct);
        return result.Status == SavedReportStatus.Ok ? Ok(result.Report) : ToError(result);
    }

    /// <summary>Deletes a saved report. Owner or admin only.</summary>
    [HttpDelete("saved/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteSaved(int id, CancellationToken ct)
    {
        var result = await _saved.DeleteAsync(id, Username, IsAdmin, ct);
        return result.Status == SavedReportStatus.Ok ? NoContent() : ToError(result);
    }

    private ObjectResult ToError(SavedReportResult result)
    {
        int status = result.Status switch
        {
            SavedReportStatus.NotFound => StatusCodes.Status404NotFound,
            SavedReportStatus.Forbidden => StatusCodes.Status403Forbidden,
            _ => StatusCodes.Status400BadRequest
        };
        return StatusCode(status, new ProblemDetails { Title = result.Error, Status = status });
    }
}
