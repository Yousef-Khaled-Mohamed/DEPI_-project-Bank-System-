using BankManagement.Application;
using BankManagement.Application.DTO;
using BankManagement.Application.IService;
using Microsoft.AspNetCore.Mvc;

namespace BankManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        var result = await authService.LoginAsync(request);
        return result is null ? Unauthorized() : Ok(result);
    }
}
