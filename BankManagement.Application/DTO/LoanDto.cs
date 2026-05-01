using System;
using System.Collections.Generic;
using System.Text;

namespace BankManagement.Application.DTO
{
    public record LoanDto(int Id, decimal Amount, decimal InterestRate, int DurationMonths, int CustomerProfileId);
}
