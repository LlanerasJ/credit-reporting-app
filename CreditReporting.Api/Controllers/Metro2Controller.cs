using System.Text;
using CreditReporting.Api.Metro2;
using CreditReporting.Shared.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CreditReporting.Api.Controllers;

[ApiController]
[Route("api/metro2")]
[Authorize]
public class Metro2Controller : ControllerBase
{
    private const long MaxUploadBytes = 10 * 1024 * 1024;

    private readonly IMetro2Service _metro2;
    public Metro2Controller(IMetro2Service metro2) => _metro2 = metro2;

    /// <summary>Lists accounts with activity in the window so the client can offer an account picker.</summary>
    [HttpGet("accounts")]
    [ProducesResponseType(typeof(List<Metro2AccountSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<Metro2AccountSummaryDto>>> Accounts(
        [FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct)
    {
        if (from is { } f && to is { } t && f > t)
            return BadRequest(new ProblemDetails { Title = "from must be on or before to." });

        return Ok(await _metro2.ListReportingAccountsAsync(from, to, ct));
    }

    /// <summary>Dry run: record count + validation issues for the selected population, no file produced.</summary>
    [HttpPost("preview")]
    [ProducesResponseType(typeof(Metro2PreviewDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<Metro2PreviewDto>> Preview(Metro2GenerateRequest request, CancellationToken ct)
    {
        if (BadRange(request) is { } problem) return problem;

        var (file, issues) = await _metro2.BuildFileAsync(request, ct);
        return Ok(new Metro2PreviewDto
        {
            RecordCount = file.BaseRecords.Count,
            ErrorCount = issues.Count(i => i.Severity == "Error"),
            WarningCount = issues.Count(i => i.Severity == "Warning"),
            Issues = issues
        });
    }

    /// <summary>Generates a Metro 2 file for the date range/account population and streams it back.</summary>
    [HttpPost("generate")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Generate(Metro2GenerateRequest request, CancellationToken ct)
    {
        if (BadRange(request) is { } problem) return problem;

        var (file, issues) = await _metro2.BuildFileAsync(request, ct);

        if (file.BaseRecords.Count == 0)
            return UnprocessableEntity(new ProblemDetails
            {
                Title = "No accounts have activity in the requested window."
            });

        if (issues.Any(i => i.Severity == "Error"))
            return UnprocessableEntity(new ValidationProblemDetails(
                issues.Where(i => i.Severity == "Error")
                      .GroupBy(i => $"record {i.RecordNumber} / {i.FieldName}")
                      .ToDictionary(g => g.Key, g => g.Select(i => i.Message).ToArray()))
            {
                Title = "Validation errors must be resolved before a file can be generated."
            });

        string content = _metro2.Serialize(file);
        string fileName = $"metro2_{(request.ToDate ?? DateTime.Today):yyyyMMdd}.dat";
        return File(Encoding.ASCII.GetBytes(content), "text/plain", fileName);
    }

    /// <summary>Parses an uploaded Metro 2 file and returns records + validation results.</summary>
    [HttpPost("parse")]
    [ProducesResponseType(typeof(Metro2ParseResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Metro2ParseResponseDto>> Parse(IFormFile? file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new ProblemDetails { Title = "Upload a non-empty Metro 2 file in the 'file' form field." });
        if (file.Length > MaxUploadBytes)
            return BadRequest(new ProblemDetails { Title = "File exceeds the 10 MB upload limit." });

        using var reader = new StreamReader(file.OpenReadStream(), Encoding.ASCII);
        string content = await reader.ReadToEndAsync(ct);

        return Ok(_metro2.ParseAndValidate(content));
    }

    private BadRequestObjectResult? BadRange(Metro2GenerateRequest request) =>
        request.FromDate is { } from && request.ToDate is { } to && from > to
            ? BadRequest(new ProblemDetails { Title = "FromDate must be on or before ToDate." })
            : null;
}
