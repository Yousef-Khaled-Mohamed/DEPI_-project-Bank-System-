using BankSystemBackend.Dto.AccountDTO;
using BankSystemBackend.Dto.CustomerDTO;
using BankSystemBackend.Dto.LoanDTO;

namespace BankSystemBackend.IRepository
{
    public interface ITellerRepo
    {
        // --- Transactions ---------------------------------------------
        Task<TransactionResponseDto> DepositAsync(
            int customerId, BankSystemBackend.Enums.AccountType accountType, decimal amount, string message = "", int? tellerId = null);

        Task<TransactionResponseDto> WithdrawAsync(
            int customerId, BankSystemBackend.Enums.AccountType accountType, decimal amount, string message = "", int? tellerId = null);

        Task<TransactionResponseDto> TransferAsync(TransactionDto dto, int? tellerId = null);

        Task<List<TransactionResponseDto>> GetTransactionHistoryAsync(int accountId, int page = 1, int pageSize = 10);

        Task<TransactionResponseDto?> GetTransactionByIdAsync(int transactionId);

        // --- Accounts -------------------------------------------------
        Task<DisplayAccount?> GetAccountByIdAsync(int accountId);

        Task<List<DisplayAccount>> GetCustomerAccountsAsync(int customerId, int page = 1, int pageSize = 10);

        Task<decimal> GetAccountBalanceAsync(int accountId);

        // --- Customer & Loans  -----------------------------
        Task<DisplayLoans> AddLoansAsync(int customerId, AddLoan dto, int? tellerId = null);
        Task<DisplayCustomer?> GetCustomerByIdAsync(int customerId);

        Task<List<DisplayLoans>> GetCustomerLoansAsync(int customerId);
    }
}


