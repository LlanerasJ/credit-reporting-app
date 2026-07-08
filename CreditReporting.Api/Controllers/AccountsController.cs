using CreditReporting.Api.Repositories;
using CreditReporting.Api.Services;
using CreditReporting.Shared.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CreditReporting.Api.Controllers;

[ApiController]
[Route("api")]
[Authorize]
public class AccountsController : ControllerBase
{
    private readonly IAccountRepository _accounts;
    private readonly ICustomerRepository _customers;

    public AccountsController(IAccountRepository accounts, ICustomerRepository customers)
    {
        _accounts = accounts;
        _customers = customers;
    }

    /// <summary>All accounts (with payment history) for a customer.</summary>
    [HttpGet("customers/{customerId:int}/accounts")]
    [ProducesResponseType(typeof(List<AccountDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<AccountDto>>> GetForCustomer(int customerId, CancellationToken ct)
    {
        if (await _customers.GetByIdAsync(customerId, ct) is null)
            return NotFound(new ProblemDetails { Title = $"Customer {customerId} not found." });

        var accounts = await _accounts.GetByCustomerAsync(customerId, ct);
        return Ok(accounts.Select(DtoMapper.ToDto).ToList());
    }

    /// <summary>One account with its full payment history.</summary>
    [HttpGet("accounts/{accountId:int}")]
    [ProducesResponseType(typeof(AccountDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AccountDto>> GetById(int accountId, CancellationToken ct)
    {
        var account = await _accounts.GetWithHistoryAsync(accountId, ct);
        return account is null
            ? NotFound(new ProblemDetails { Title = $"Account {accountId} not found." })
            : Ok(DtoMapper.ToDto(account));
    }
}
