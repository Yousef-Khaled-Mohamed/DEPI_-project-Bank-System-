using BankSystemBackend.Dto.BranchDto;
using BankSystemBackend.Dto.CustomerDTO;
using BankSystemBackend.Dto.TellerDTO;
using BankSystemBackend.IRepository;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using BankSystemBackend.Dto.AccountDTO;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace BankSystemBackend.Controllers
{
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly IAdminRepo _adminRepo;

        public AdminController(IAdminRepo adminRepo)
        {
            _adminRepo = adminRepo;
        }

        // --- Customer reads -------------------------------------------

        [HttpGet("customers")]
        public async Task<IActionResult> GetAllCustomers(
            [FromQuery] string? search = null,
            [FromQuery] string? status = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var customers = await _adminRepo.GetAllCustomersAsync(search, status, page, pageSize);
            return Ok(customers);
        }

        [HttpGet("customers/{id:int}")]
        public async Task<IActionResult> GetCustomerById(int id)
        {
            var customer = await _adminRepo.GetCustomerByIdAsync(id);
            if (customer is null) return NotFound($"Customer {id} not found.");
            return Ok(customer);
        }

        [HttpGet("customers/count")]
        public async Task<IActionResult> GetCustomerCount(
            [FromQuery] string? search = null,
            [FromQuery] string? status = null)
        {
            var count = await _adminRepo.GetCustomerCountAsync(search, status);
            return Ok(new { Count = count });
        }

        // --- Customer write -------------------------------------------
        [HttpPost("customers/{id:int}/accounts")]
        public async Task<IActionResult> AddAccount(int id, [FromBody] CreateAccount dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                var account = await _adminRepo.AddAccountAsync(id, dto);
                return CreatedAtAction(nameof(GetCustomerById), new { id = id }, account);
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

        [HttpPost("customers")]
        public async Task<IActionResult> AddCustomer([FromBody] AddCustomer dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var created = await _adminRepo.AddCustomerAsync(dto);
            return CreatedAtAction(nameof(GetCustomerById), new { id = created.Id }, created);
        }

        [HttpPut("customers/{id:int}")]
        public async Task<IActionResult> UpdateCustomer(int id, [FromBody] EditCustomer dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                var updated = await _adminRepo.UpdateCustomerAsync(id, dto);
                return Ok(updated);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpDelete("customers/{id:int}")]
        public async Task<IActionResult> DeleteCustomer(int id)
        {
            var deleted = await _adminRepo.DeleteCustomerAsync(id);
            if (!deleted) return NotFound($"Customer {id} not found.");
            return NoContent();
        }

        // --- Aggregate statistics -------------------------------------

        [HttpGet("stats/deposits")]
        public async Task<IActionResult> GetTotalDeposits()
        {
            var total = await _adminRepo.GetTotalDepositsAsync();
            return Ok(new { TotalDeposits = total });
        }

        [HttpGet("stats/loans")]
        public async Task<IActionResult> GetTotalLoans()
        {
            var total = await _adminRepo.GetTotalLoansAsync();
            return Ok(new { TotalLoans = total });
        }

        [HttpGet("stats/balance")]
        public async Task<IActionResult> GetTotalAmount()
        {
            var total = await _adminRepo.TotalAmountAsync();
            return Ok(new { TotalBalance = total });
        }

        [HttpGet("stats/fees")]
        public async Task<IActionResult> GetTotalFees()
        {
            var total = await _adminRepo.GetTotalFeesAsync();
            return Ok(new { TotalFees = total });
        }

        [HttpGet("stats/withdrawals")]
        public async Task<IActionResult> GetTotalWithdrawals()
        {
            var total = await _adminRepo.TotalWithdrawalsAsync();
            return Ok(new { TotalWithdrawals = total });
        }

        [HttpGet("stats/accounts-count")]
        public async Task<IActionResult> GetTotalAccountsCount()
        {
            var count = await _adminRepo.GetTotalAccountsCountAsync();
            return Ok(new { Count = count });
        }

        [HttpGet("stats/cards-count")]
        public async Task<IActionResult> GetTotalCardsCount()
        {
            var count = await _adminRepo.GetTotalCardsCountAsync();
            return Ok(new { Count = count });
        }

        // --- Teller CRUD ----------------------------------------------

        [HttpGet("tellers")]
        public async Task<IActionResult> GetAllTellers(
            [FromQuery] string? search = null,
            [FromQuery] string? status = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var tellers = await _adminRepo.GetAllTellersAsync(search, status, page, pageSize);
            return Ok(tellers);
        }

        [HttpGet("tellers/{id:int}")]
        public async Task<IActionResult> GetTellerById(int id)
        {
            var teller = await _adminRepo.GetTellerByIdAsync(id);
            if (teller is null) return NotFound($"Teller {id} not found.");
            return Ok(teller);
        }

        [HttpGet("tellers/count")]
        public async Task<IActionResult> GetTellerCount(
            [FromQuery] string? search = null,
            [FromQuery] string? status = null)
        {
            var count = await _adminRepo.GetTellerCountAsync(search, status);
            return Ok(new { Count = count });
        }

        [HttpPost("tellers")]
        public async Task<IActionResult> AddTeller([FromBody] AddTeller dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var created = await _adminRepo.AddTellerAsync(dto);
            return CreatedAtAction(nameof(GetTellerById), new { id = created.Id }, created);
        }

        [HttpPut("tellers/{id:int}")]
        public async Task<IActionResult> UpdateTeller(int id, [FromBody] EditTeller dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                var updated = await _adminRepo.UpdateTellerAsync(id, dto);
                return Ok(updated);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpDelete("tellers/{id:int}")]
        public async Task<IActionResult> DeleteTeller(int id)
        {
            var deleted = await _adminRepo.DeleteTellerAsync(id);
            if (!deleted) return NotFound($"Teller {id} not found.");
            return NoContent();
        }

        // --- Branch CRUD ----------------------------------------------

        [HttpGet("branches")]
        public async Task<IActionResult> GetAllBranches()
        {
            var branches = await _adminRepo.GetAllBranchesAsync();
            return Ok(branches);
        }

        [HttpGet("branches/{id}")]
        public async Task<IActionResult> GetBranchById(int id)
        {
            var branch = await _adminRepo.GetBranchByIdAsync(id);
            if (branch is null) return NotFound($"Branch {id} not found.");
            return Ok(branch);
        }

        [HttpGet("branches/count")]
        public async Task<IActionResult> GetBranchCount()
        {
            var count = await _adminRepo.GetBranchCountAsync();
            return Ok(new { Count = count });
        }

        [HttpPost("branches")]
        public async Task<IActionResult> AddBranch([FromBody] AddBranch dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var created = await _adminRepo.AddBranchAsync(dto);
            return CreatedAtAction(nameof(GetBranchById), new { id = created.Id }, created);
        }

        [HttpPut("branches/{id}")]
        public async Task<IActionResult> UpdateBranch(int id, [FromBody] EditBranch dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                var updated = await _adminRepo.UpdateBranchAsync(id, dto);
                return Ok(updated);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpDelete("branches/{id}")]
        public async Task<IActionResult> DeleteBranch(int id)
        {
            var deleted = await _adminRepo.DeleteBranchAsync(id);
            if (!deleted) return NotFound($"Branch {id} not found.");
            return NoContent();
        }
    }
}
