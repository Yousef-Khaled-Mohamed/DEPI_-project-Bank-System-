using BankSystemBackend.Dto.AccountDTO;
using BankSystemBackend.Dto.CustomerDTO;
using BankSystemBackend.Dto.LoanDTO;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BankSystemBackend.IRepository
{
    public interface ICustomerRepo
    {
        // --- Transactions ---------------------------------------------
        Task<TransactionResponseDto> TransferAsync(TransactionDto dto);

        Task<List<TransactionResponseDto>> GetAllTransactionsAsync(int accountId, int page = 1, int pageSize = 10);

        // --- Profile --------------------------------------------------
        Task<DisplayCustomer?> GetProfileAsync(int customerId);

        Task<DisplayCustomer> UpdateProfileAsync(int customerId, EditCustomer Profile);

        Task<Microsoft.AspNetCore.Identity.IdentityResult> ChangePasswordAsync(int customerId, BankSystemBackend.Dto.CustomerDTO.ChangePasswordDto dto);

        // --- Accounts -------------------------------------------------
        Task<List<DisplayAccount>> GetMyAccountsAsync(int customerId, int page = 1, int pageSize = 10);

        Task<bool> OwnsAccountAsync(int customerId, int accountId);

        Task<decimal> GetAccountBalanceAsync(int accountId);

        // New method required by User Request: Bank Account System self-creation
        Task<DisplayAccount> CreateAccountForMeAsync(int customerId, CreateAccount dto);

        // --- Loans ----------------------------------------------------
        Task<List<DisplayLoans>> GetMyLoansAsync(int customerId);

        Task<DisplayLoans?> GetLoanByIdAsync(int loanId);
    }
}
