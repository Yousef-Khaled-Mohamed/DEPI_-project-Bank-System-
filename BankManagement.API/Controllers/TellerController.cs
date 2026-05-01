using System.Security.Claims;
using BankManagement.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BankManagement.Application.DTO;
using BankManagement.Application.IService;
namespace BankManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = RoleNames.Teller)]
public class TellerController(ITellerService tellerService) : ControllerBase
{
    [HttpPost("customers")]
    public async Task<IActionResult> CreateCustomer([FromBody] CreateCustomerDto request)
        => Ok(await tellerService.CreateCustomerAsync(GetUserId(), request));

    [HttpPost("accounts")]
    public async Task<IActionResult> CreateAccount([FromBody] CreateAccountDto request)
        => Ok(await tellerService.CreateAccountAsync(GetUserId(), request));

    [HttpPost("deposit")]
    public async Task<IActionResult> Deposit([FromBody] AmountOperationDto request)
        => Ok(await tellerService.DepositAsync(GetUserId(), request));

    [HttpPost("withdraw")]
    public async Task<IActionResult> Withdraw([FromBody] AmountOperationDto request)
        => Ok(await tellerService.WithdrawAsync(GetUserId(), request));

    [HttpPost("loans")]
    public async Task<IActionResult> CreateLoan([FromBody] CreateLoanDto request)
        => Ok(await tellerService.CreateLoanAsync(GetUserId(), request));

    private string GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new UnauthorizedAccessException();
}
