using System;
using System.Collections.Generic;
using System.Text;

namespace BankManagement.Application.DTO
{
    public record CustomerDto(int Id, string IdentityUserId, string CreatedByTellerId);
}
