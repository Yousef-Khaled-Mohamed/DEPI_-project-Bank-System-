using BankManagement.Application.DTO;
using System;
using System.Collections.Generic;
using System.Text;

namespace BankManagement.Application.IService
{
    public interface IAdminService
    {
        Task<string> CreateTellerAsync(CreateTellerDto request);
        Task<IEnumerable<TransactionDto>> GetAllTransactionsAsync();
        Task<SystemSummaryDto> GetSummaryAsync();
        Task<BranchDto> CreateBranchAsync(BranchDto dto);
        Task<BranchDto?> UpdateBranchAsync(int id, BranchDto dto);
        Task<bool> DeleteBranchAsync(int id);

        Task<IEnumerable<TellerDto>> GetAllTellersAsync();
    }

}
