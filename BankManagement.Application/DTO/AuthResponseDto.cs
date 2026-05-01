using System;
using System.Collections.Generic;
using System.Text;

namespace BankManagement.Application.DTO
{
    public record AuthResponseDto(string Token, DateTime ExpiresAt, string UserId, string Email, IEnumerable<string> Roles);
}
