using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BankManagement.Application.DTO
{
    public record TellerDto(
        string Id,
        string UserName,
        string Email,
        string PhoneNumber
    );
}
