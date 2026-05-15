using BankManagement.Application.DTO;
using System;
using System.Collections.Generic;
using System.Text;

namespace BankManagement.Application.IService
{
    public interface ITellerService
    {
        Task<CustomerDto> CreateCustomerAsync(string tellerUserId, CreateCustomerDto request);
        Task<AccountDto> CreateAccountAsync(string tellerUserId, CreateAccountDto request);
        Task<TransactionDto> DepositAsync(string tellerUserId, AmountOperationDto request);
        Task<TransactionDto> WithdrawAsync(string tellerUserId, AmountOperationDto request);
        Task<LoanDto> CreateLoanAsync(string tellerUserId, CreateLoanDto request);
        Task<IEnumerable<AccountDto>> GetCustomerAccountsAsync(int customerId); 
    }
}
