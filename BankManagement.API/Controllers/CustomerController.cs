using System.Security.Claims;
using BankManagement.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BankManagement.Application;
using BankManagement.Application.DTO;
using BankManagement.Application.IService;

namespace BankManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = RoleNames.Customer)]
public class CustomerController(ICustomerService customerService) : ControllerBase
{
    [HttpGet("accounts")]
    public async Task<IActionResult> Accounts() => Ok(await customerService.GetAccountsAsync(GetUserId()));

    [HttpGet("transactions")]
    public async Task<IActionResult> Transactions() => Ok(await customerService.GetTransactionsAsync(GetUserId()));

    [HttpGet("loans")]
    public async Task<IActionResult> Loans() => Ok(await customerService.GetLoansAsync(GetUserId()));

    [HttpPost("transfer")]
    public async Task<IActionResult> Transfer([FromBody] TransferDto request)
        => Ok(await customerService.TransferAsync(GetUserId(), request));

    private string GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new UnauthorizedAccessException();
}
