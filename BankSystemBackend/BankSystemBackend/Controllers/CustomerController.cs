using BankSystemBackend.Dto.CustomerDTO;
using BankSystemBackend.IRepository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using BankSystemBackend.Dto.AccountDTO;

namespace BankSystemBackend.Controllers
{
    [Route("api/[controller]")]
    [Authorize(Roles = "Customer")]
    [ApiController]
    public class CustomerController : ControllerBase
    {
        private readonly ICustomerRepo _customerRepo;

        public CustomerController(ICustomerRepo customerRepo)
        {
            _customerRepo = customerRepo;
        }

        private int GetUserId()
        {
            var val = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(val, out var id) ? id : 0;
        }

        // --- Profile --------------------------------------------------

        [HttpGet("{customerId:int}/profile")]
        public async Task<IActionResult> GetProfile(int customerId)
        {
            if (GetUserId() != customerId) return Forbid();
            var profile = await _customerRepo.GetProfileAsync(customerId);
            if (profile is null) return NotFound($"Customer {customerId} not found.");
            return Ok(profile);
        }

        [HttpPut("{customerId:int}/profile")]
        public async Task<IActionResult> UpdateProfile(int customerId, [FromBody] EditCustomer dto)
        {
            if (GetUserId() != customerId) return Forbid();
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                var updated = await _customerRepo.UpdateProfileAsync(customerId, dto);
                return Ok(updated);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpPut("{customerId:int}/change-password")]
        public async Task<IActionResult> ChangePassword(int customerId, [FromBody] ChangePasswordDto dto)
        {
            if (GetUserId() != customerId) return Forbid();
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var result = await _customerRepo.ChangePasswordAsync(customerId, dto);
            if (!result.Succeeded) return BadRequest(result.Errors);
            return Ok(new { Message = "Password changed successfully." });
        }

        // --- Accounts -------------------------------------------------

        [HttpGet("{customerId:int}/accounts")]
        public async Task<IActionResult> GetMyAccounts(int customerId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            if (GetUserId() != customerId) return Forbid();
            var accounts = await _customerRepo.GetMyAccountsAsync(customerId, page, pageSize);
            return Ok(accounts);
        }

        [HttpPost("{customerId:int}/accounts")]
        public async Task<IActionResult> CreateAccount(int customerId, [FromBody] CreateAccount dto)
        {
            if (GetUserId() != customerId) return Forbid();
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                var account = await _customerRepo.CreateAccountForMeAsync(customerId, dto);
                return CreatedAtAction(nameof(GetMyAccounts), new { customerId = customerId }, account);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("accounts/{accountId:int}/balance")]
        public async Task<IActionResult> GetAccountBalance(int accountId)
        {
            if (!await _customerRepo.OwnsAccountAsync(GetUserId(), accountId)) return Forbid();

            try
            {
                var balance = await _customerRepo.GetAccountBalanceAsync(accountId);
                return Ok(new { AccountId = accountId, Balance = balance });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        // --- Transactions ---------------------------------------------

        [HttpPost("transfer")]
        public async Task<IActionResult> Transfer([FromBody] TransactionDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            if (!await _customerRepo.OwnsAccountAsync(GetUserId(), dto.AccountId)) return Forbid();

            try
            {
                var result = await _customerRepo.TransferAsync(dto);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("accounts/{accountId:int}/transactions")]
        public async Task<IActionResult> GetAllTransactions(int accountId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            if (!await _customerRepo.OwnsAccountAsync(GetUserId(), accountId)) return Forbid();
            var transactions = await _customerRepo.GetAllTransactionsAsync(accountId, page, pageSize);
            return Ok(transactions);
        }

        // --- Loans ----------------------------------------------------

        [HttpGet("{customerId:int}/loans")]
        public async Task<IActionResult> GetMyLoans(int customerId)
        {
            if (GetUserId() != customerId) return Forbid();
            var loans = await _customerRepo.GetMyLoansAsync(customerId);
            return Ok(loans);
        }

        [HttpGet("loans/{loanId:int}")]
        public async Task<IActionResult> GetLoanById(int loanId)
        {
            var loan = await _customerRepo.GetLoanByIdAsync(loanId);
            if (loan is null) return NotFound($"Loan {loanId} not found.");
            if (loan.CustomerId != GetUserId()) return Forbid();
            
            return Ok(loan);
        }
    }
}

