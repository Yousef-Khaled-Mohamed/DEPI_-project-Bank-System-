using BankManagement.Application.DTO;
using System;
using System.Collections.Generic;
using System.Text;

namespace BankManagement.Application.IService
{
    public interface IAuthService
    {
        Task<AuthResponseDto?> LoginAsync(LoginRequestDto request);
    }
}
public static class RoleNames
{
    public const string Admin = "Admin";
    public const string Teller = "Teller";
    public const string Customer = "Customer";
}


