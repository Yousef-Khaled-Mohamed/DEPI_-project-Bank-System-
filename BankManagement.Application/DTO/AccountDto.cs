using BankManagement.Domain.Enum;
using System;
using System.Collections.Generic;
using System.Text;

namespace BankManagement.Application.DTO
{
    public record AccountDto(int Id, string AccountNumber, AccountType AccountType, decimal Balance, string CardNumber, DateTime CardExpiryDate);

}
