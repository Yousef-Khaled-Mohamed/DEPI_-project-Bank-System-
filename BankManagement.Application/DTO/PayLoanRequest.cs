using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BankManagement.Application.DTO
{
    public record PayLoanRequest(int LoanId, decimal Amount);
}
