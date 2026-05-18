using BankSystemBackend.Dto.AccountDTO;
using BankSystemBackend.Dto.CustomerDTO;
using BankSystemBackend.Dto.LoanDTO;
using BankSystemBackend;
using BankSystemBackend.IRepository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BankSystemBackend.Controllers
{
    [Route("api/[controller]")]
    [Authorize(Roles = "Teller")]
    [ApiController]
    public class TellerController : ControllerBase
    {
        private readonly ITellerRepo _tellerRepo;

        public TellerController(ITellerRepo tellerRepo)
        {
            _tellerRepo = tellerRepo;
        }

        private int GetUserId()
        {
            var val = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(val, out var id) ? id : 0;
        }

        // --- Transactions ---------------------------------------------

        [HttpPost("deposit")]
        public async Task<IActionResult> Deposit(
            [FromQuery] int customerId,
            [FromQuery] BankSystemBackend.Enums.AccountType accountType,
            [FromQuery] decimal amount,
            [FromQuery] string message = "")
        {
            if (amount <= 0) return BadRequest("Amount must be greater than zero.");

            try
            {
                var tellerId = GetUserId();
                var result = await _tellerRepo.DepositAsync(customerId, accountType, amount, message, tellerId);
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

        [HttpPost("withdraw")]
        public async Task<IActionResult> Withdraw(
            [FromQuery] int customerId,
            [FromQuery] BankSystemBackend.Enums.AccountType accountType,
            [FromQuery] decimal amount,
            [FromQuery] string message = "")
        {
            if (amount <= 0) return BadRequest("Amount must be greater than zero.");

            try
            {
                var tellerId = GetUserId();
                var result = await _tellerRepo.WithdrawAsync(customerId, accountType, amount, message, tellerId);
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

        [HttpPost("transfer")]
        public async Task<IActionResult> Transfer([FromBody] TransactionDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                var tellerId = GetUserId();
                var result = await _tellerRepo.TransferAsync(dto, tellerId);
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
        public async Task<IActionResult> GetTransactionHistory(int accountId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var history = await _tellerRepo.GetTransactionHistoryAsync(accountId, page, pageSize);
            return Ok(history);
        }

        [HttpGet("transactions/{transactionId:int}")]
        public async Task<IActionResult> GetTransactionById(int transactionId)
        {
            var transaction = await _tellerRepo.GetTransactionByIdAsync(transactionId);
            if (transaction is null) return NotFound($"Transaction {transactionId} not found.");
            return Ok(transaction);
        }

        // --- Accounts -------------------------------------------------

        [HttpGet("accounts/{accountId:int}")]
        public async Task<IActionResult> GetAccountById(int accountId)
        {
            var account = await _tellerRepo.GetAccountByIdAsync(accountId);
            if (account is null) return NotFound($"Account {accountId} not found.");
            return Ok(account);
        }

        [HttpGet("customers/{customerId:int}/accounts")]
        public async Task<IActionResult> GetCustomerAccounts(int customerId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var accounts = await _tellerRepo.GetCustomerAccountsAsync(customerId, page, pageSize);
            return Ok(accounts);
        }

        [HttpGet("accounts/{accountId:int}/balance")]
        public async Task<IActionResult> GetAccountBalance(int accountId)
        {
            try
            {
                var balance = await _tellerRepo.GetAccountBalanceAsync(accountId);
                return Ok(new { AccountId = accountId, Balance = balance });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        // --- Customer & Loans -----------------------------------------

        [HttpGet("customers/{customerId:int}")]
        public async Task<IActionResult> GetCustomerById(int customerId)
        {
            var customer = await _tellerRepo.GetCustomerByIdAsync(customerId);
            if (customer is null) return NotFound($"Customer {customerId} not found.");
            return Ok(customer);
        }

        [HttpPost("customers/{customerId:int}/loans")]
        public async Task<IActionResult> AddLoan(int customerId, [FromBody] AddLoan dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                dto.CustomerId = customerId;
                var tellerId = GetUserId();
                var loan = await _tellerRepo.AddLoansAsync(customerId, dto, tellerId);
                return Ok(loan);
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

        [HttpGet("customers/{customerId:int}/loans")]
        public async Task<IActionResult> GetCustomerLoans(int customerId)
        {
            var loans = await _tellerRepo.GetCustomerLoansAsync(customerId);
            return Ok(loans);
        }
    }
}
