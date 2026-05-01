using BankManagement.Application;
using BankManagement.Application.DTO;
using BankManagement.Application.IService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BankManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = RoleNames.Admin)]
public class AdminController(IAdminService adminService) : ControllerBase
{
    [HttpPost("tellers")]
    public async Task<IActionResult> CreateTeller([FromBody] CreateTellerDto request)
        => Ok(new { tellerId = await adminService.CreateTellerAsync(request) });

    [HttpGet("transactions")]
    public async Task<IActionResult> Transactions() => Ok(await adminService.GetAllTransactionsAsync());

    [HttpGet("summary")]
    public async Task<IActionResult> Summary() => Ok(await adminService.GetSummaryAsync());

    [HttpPost("branches")]
    public async Task<IActionResult> CreateBranch([FromBody] BranchDto dto) => Ok(await adminService.CreateBranchAsync(dto));

    [HttpPut("branches/{id:int}")]
    public async Task<IActionResult> UpdateBranch(int id, [FromBody] BranchDto dto)
    {
        var result = await adminService.UpdateBranchAsync(id, dto);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpDelete("branches/{id:int}")]
    public async Task<IActionResult> DeleteBranch(int id)
        => await adminService.DeleteBranchAsync(id) ? NoContent() : NotFound();
}
