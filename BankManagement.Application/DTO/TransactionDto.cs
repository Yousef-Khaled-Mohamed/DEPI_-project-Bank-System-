using BankManagement.Domain.Enum;
using System;
using System.Collections.Generic;
using System.Text;

namespace BankManagement.Application.DTO
{
    public record TransactionDto(int Id, int AccountId, decimal Amount, TransactionType Type, DateTime Date, int? TargetAccountId);
}
