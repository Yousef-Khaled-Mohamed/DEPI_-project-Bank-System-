using BankSystemBackend.Dto.BranchDto;
using BankSystemBackend.Dto.CustomerDTO;
using BankSystemBackend.Dto.TellerDTO;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BankSystemBackend.IRepository
{
    public interface IAdminRepo
    {
        // --- Customer reads -------------------------------------------
        Task<List<DisplayCustomer>> GetAllCustomersAsync(string? search = null, string? status = null, int page = 1, int pageSize = 10);
        Task<DisplayCustomer?> GetCustomerByIdAsync(int id);
        Task<int> GetCustomerCountAsync(string? search = null, string? status = null);

        // --- Customer write -------------------------------------------
        Task<BankSystemBackend.Dto.AccountDTO.DisplayAccount> AddAccountAsync(int customerId, BankSystemBackend.Dto.AccountDTO.CreateAccount dto);
        Task<DisplayCustomer> AddCustomerAsync(AddCustomer customer);
        Task<DisplayCustomer> UpdateCustomerAsync(int id, EditCustomer dto);
        Task<bool> DeleteCustomerAsync(int id);

        // --- Aggregate statistics -------------------------------------
        Task<decimal> GetTotalDepositsAsync();
        Task<decimal> GetTotalLoansAsync();
        Task<decimal> TotalAmountAsync();
        Task<decimal> GetTotalFeesAsync();
        Task<decimal> TotalWithdrawalsAsync();
        
        // New statistics required by User Request
        Task<int> GetTotalAccountsCountAsync();
        Task<int> GetTotalCardsCountAsync();

        // --- Teller CRUD ----------------------------------------------
        Task<DisplayTeller?> GetTellerByIdAsync(int id);
        Task<List<DisplayTeller>> GetAllTellersAsync(string? search = null, string? status = null, int page = 1, int pageSize = 10);
        Task<int> GetTellerCountAsync(string? search = null, string? status = null);
        Task<DisplayTeller> AddTellerAsync(AddTeller teller);
        Task<DisplayTeller> UpdateTellerAsync(int id, EditTeller teller);
        Task<bool> DeleteTellerAsync(int id);

        // --- Branch CRUD ----------------------------------------------
        Task<DisplayBranch> AddBranchAsync(AddBranch branch);
        Task<DisplayBranch> UpdateBranchAsync(int id, EditBranch branch);
        Task<bool> DeleteBranchAsync(int id);
        Task<List<DisplayBranch>> GetAllBranchesAsync();
        Task<DisplayBranch?> GetBranchByIdAsync(int id);
        Task<int> GetBranchCountAsync();
    }
}
