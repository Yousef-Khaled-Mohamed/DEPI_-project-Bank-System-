using System;
using System.Collections.Generic;
using System.Text;

namespace BankManagement.Application.DTO
{
    public record SystemSummaryDto(decimal TotalBalance, int TotalTransactions, decimal TotalTransfersAmount);
}
