using CreditReporting.Api.Repositories;
using CreditReporting.Api.Services;
using CreditReporting.Shared.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CreditReporting.Api.Controllers;

[ApiController]
[Route("api/customers")]
[Authorize]
public class CustomersController : ControllerBase
{
    private readonly ICustomerRepository _customers;
    public CustomersController(ICustomerRepository customers) => _customers = customers;

    /// <summary>Search customers by (partial) name and/or last 4 of SSN.</summary>
    [HttpGet("search")]
    [ProducesResponseType(typeof(List<CustomerSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<CustomerSummaryDto>>> Search(
        [FromQuery] string? name, [FromQuery] string? ssnLast4, [FromQuery] string? state, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(ssnLast4) && string.IsNullOrWhiteSpace(state))
            return BadRequest(new ProblemDetails { Title = "Provide a name, the last 4 digits of an SSN, or a state." });

        if (!string.IsNullOrWhiteSpace(ssnLast4) &&
            (ssnLast4.Trim().Length != 4 || !ssnLast4.Trim().All(char.IsDigit)))
            return BadRequest(new ProblemDetails { Title = "SSN must be exactly 4 digits." });

        if (!string.IsNullOrWhiteSpace(state) &&
            (state.Trim().Length != 2 || !state.Trim().All(char.IsLetter)))
            return BadRequest(new ProblemDetails { Title = "State must be exactly 2 characters." });

        var results = await _customers.SearchAsync(name, ssnLast4, state, ct);
        return Ok(results.Select(DtoMapper.ToSummary).ToList());
    }

    /// <summary>Customer detail (masked SSN) by id.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(CustomerDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CustomerDetailDto>> GetById(int id, CancellationToken ct)
    {
        var customer = await _customers.GetByIdAsync(id, ct);
        return customer is null
            ? NotFound(new ProblemDetails { Title = $"Customer {id} not found." })
            : Ok(DtoMapper.ToDetail(customer));
    }
}
