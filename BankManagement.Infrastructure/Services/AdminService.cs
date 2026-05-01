using AutoMapper;
using BankManagement.Application;
using BankManagement.Application.DTO;
using BankManagement.Application.IService;
using BankManagement.Domain.Entities;
using BankManagement.Domain.Enum;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace BankManagement.Infrastructure.Services
{
    public class AdminService(
    AppDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager,
    IMapper mapper) : IAdminService
    {
        public async Task<string> CreateTellerAsync(CreateTellerDto request)
        {
            var teller = new ApplicationUser { UserName = request.Email, Email = request.Email, EmailConfirmed = true };
            var result = await userManager.CreateAsync(teller, request.Password);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(string.Join(";", result.Errors.Select(e => e.Description)));
            }

            if (!await roleManager.RoleExistsAsync(RoleNames.Teller))
            {
                await roleManager.CreateAsync(new IdentityRole(RoleNames.Teller));
            }

            await userManager.AddToRoleAsync(teller, RoleNames.Teller);
            return teller.Id;
        }

        public async Task<IEnumerable<TransactionDto>> GetAllTransactionsAsync()
        {
            var txs = await dbContext.Transactions.OrderByDescending(x => x.Date).ToListAsync();
            return mapper.Map<IEnumerable<TransactionDto>>(txs);
        }

        public async Task<SystemSummaryDto> GetSummaryAsync()
        {
            return new SystemSummaryDto(
                await dbContext.Accounts.SumAsync(x => x.Balance),
                await dbContext.Transactions.CountAsync(),
                await dbContext.Transactions.Where(x => x.Type == TransactionType.Transfer).SumAsync(x => x.Amount));
        }

        public async Task<BranchDto> CreateBranchAsync(BranchDto dto)
        {
            var branch = mapper.Map<Branch>(dto);
            dbContext.Branches.Add(branch);
            await dbContext.SaveChangesAsync();
            return mapper.Map<BranchDto>(branch);
        }

        public async Task<BranchDto?> UpdateBranchAsync(int id, BranchDto dto)
        {
            var branch = await dbContext.Branches.FindAsync(id);
            if (branch is null) return null;
            branch.Name = dto.Name;
            branch.Address = dto.Address;
            await dbContext.SaveChangesAsync();
            return mapper.Map<BranchDto>(branch);
        }

        public async Task<bool> DeleteBranchAsync(int id)
        {
            var branch = await dbContext.Branches.FindAsync(id);
            if (branch is null) return false;
            dbContext.Branches.Remove(branch);
            await dbContext.SaveChangesAsync();
            return true;
        }
    }

}
