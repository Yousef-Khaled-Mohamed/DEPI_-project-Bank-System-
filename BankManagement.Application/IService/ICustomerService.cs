using BankManagement.Application.DTO;
using System;
using System.Collections.Generic;
using System.Text;

namespace BankManagement.Application.IService
{
    public interface ICustomerService
    {
        Task<IEnumerable<AccountDto>> GetAccountsAsync(string customerUserId);
        Task<IEnumerable<TransactionDto>> GetTransactionsAsync(string customerUserId);
        Task<IEnumerable<LoanDto>> GetLoansAsync(string customerUserId);
        Task<TransactionDto> TransferAsync(string customerUserId, TransferDto request);
    }
}
