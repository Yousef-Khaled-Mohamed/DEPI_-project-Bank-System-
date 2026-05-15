using BankManagement.Application;
using BankManagement.Application.DTO;
using BankManagement.Application.IService;
using BankManagement.Domain.Entities;
using BankManagement.Domain.Enum;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace BankManagement.Infrastructure.Services
{
    public class TellerService(
       AppDbContext dbContext,
       UserManager<ApplicationUser> userManager,
       RoleManager<IdentityRole> roleManager,
       ILogger<TellerService> logger) : ITellerService
    {
        public async Task<CustomerDto> CreateCustomerAsync(string tellerUserId, CreateCustomerDto request)
        {
            var customerUser = new ApplicationUser { UserName = request.Email, Email = request.Email, EmailConfirmed = true };
            var result = await userManager.CreateAsync(customerUser, request.Password);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(string.Join(";", result.Errors.Select(e => e.Description)));
            }

            if (!await roleManager.RoleExistsAsync(RoleNames.Customer))
            {
                await roleManager.CreateAsync(new IdentityRole(RoleNames.Customer));
            }

            await userManager.AddToRoleAsync(customerUser, RoleNames.Customer);
            var profile = new CustomerProfile { IdentityUserId = customerUser.Id, CreatedByTellerId = tellerUserId };
            dbContext.CustomerProfiles.Add(profile);
            await dbContext.SaveChangesAsync();
            return new CustomerDto(profile.Id, profile.IdentityUserId, profile.CreatedByTellerId);
        }

        public async Task<AccountDto> CreateAccountAsync(string tellerUserId, CreateAccountDto request)
        {
            var customer = await dbContext.CustomerProfiles.FirstOrDefaultAsync(x => x.Id == request.CustomerProfileId && x.CreatedByTellerId == tellerUserId)
                ?? throw new UnauthorizedAccessException("Teller can manage only their own customers.");

            var account = new BankAccount
            {
                CustomerProfileId = customer.Id,
                AccountNumber = $"ACC-{RandomNumberGenerator.GetInt32(10000000, 99999999)}",
                AccountType = request.AccountType,
                Balance = 0
            };
            account.Card = new Card
            {
                CardNumber = $"{RandomNumberGenerator.GetInt32(1000, 9999)}{RandomNumberGenerator.GetInt32(1000, 9999)}{RandomNumberGenerator.GetInt32(1000, 9999)}{RandomNumberGenerator.GetInt32(1000, 9999)}",
                Cvv = RandomNumberGenerator.GetInt32(100, 1000).ToString(),
                ExpiryDate = DateTime.UtcNow.AddYears(5)
            };

            dbContext.Accounts.Add(account);
            await dbContext.SaveChangesAsync();
            return new AccountDto(account.Id, account.AccountNumber, account.AccountType, account.Balance, account.Card.CardNumber, account.Card.ExpiryDate);
        }

        public async Task<TransactionDto> DepositAsync(string tellerUserId, AmountOperationDto request)
        {
            var account = await GetOwnedAccountAsync(tellerUserId, request.AccountId);
            account.Balance += request.Amount;
            var tx = new BankTransaction { AccountId = account.Id, Amount = request.Amount, Type = TransactionType.Deposit, Date = DateTime.UtcNow };
            dbContext.Transactions.Add(tx);
            await dbContext.SaveChangesAsync();
            logger.LogInformation("Deposit made. Teller:{TellerId} Account:{AccountId} Amount:{Amount}", tellerUserId, account.Id, request.Amount);
            return new TransactionDto(tx.Id, tx.AccountId, tx.Amount, tx.Type, tx.Date, null);
        }

        public async Task<TransactionDto> WithdrawAsync(string tellerUserId, AmountOperationDto request)
        {
            var account = await GetOwnedAccountAsync(tellerUserId, request.AccountId);
            if (account.Balance < request.Amount) throw new InvalidOperationException("Insufficient balance.");
            account.Balance -= request.Amount;
            var tx = new BankTransaction { AccountId = account.Id, Amount = request.Amount, Type = TransactionType.Withdraw, Date = DateTime.UtcNow };
            dbContext.Transactions.Add(tx);
            await dbContext.SaveChangesAsync();
            logger.LogInformation("Withdrawal made. Teller:{TellerId} Account:{AccountId} Amount:{Amount}", tellerUserId, account.Id, request.Amount);
            return new TransactionDto(tx.Id, tx.AccountId, tx.Amount, tx.Type, tx.Date, null);
        }

        public async Task<LoanDto> CreateLoanAsync(string tellerUserId, CreateLoanDto request)
        {
            var customer = await dbContext.CustomerProfiles.FirstOrDefaultAsync(x => x.Id == request.CustomerProfileId && x.CreatedByTellerId == tellerUserId)
                ?? throw new UnauthorizedAccessException("Teller can manage only their own customers.");
            var loan = new Loan
            {
                CustomerProfileId = customer.Id,
                Amount = request.Amount,
                InterestRate = request.InterestRate,
                DurationMonths = request.DurationMonths
            };
            dbContext.Loans.Add(loan);
            await dbContext.SaveChangesAsync();
            logger.LogInformation("Loan created. Teller:{TellerId} Customer:{CustomerId} Amount:{Amount}", tellerUserId, customer.Id, loan.Amount);
            return new LoanDto(loan.Id, loan.Amount, loan.InterestRate, loan.DurationMonths, loan.CustomerProfileId);
        }

        private async Task<BankAccount> GetOwnedAccountAsync(string tellerUserId, int accountId)
        {
            return await dbContext.Accounts.Include(x => x.CustomerProfile).FirstOrDefaultAsync(x => x.Id == accountId && x.CustomerProfile.CreatedByTellerId == tellerUserId)
                ?? throw new UnauthorizedAccessException("Teller can manage only their own customers.");
        }

         public async Task<IEnumerable<AccountDto>> GetCustomerAccountsAsync(int customerId)
         {
             var accounts = await dbContext.Accounts.Include(a => a.Card).Where(a => a.CustomerProfileId == customerId).ToListAsync();
             if (accounts == null || !accounts.Any())
             {
                 return Enumerable.Empty<AccountDto>(); 
             }
        
             var accountDtos = accounts.Select(a => new AccountDto(
                 a.Id,
                 a.AccountNumber,
                 a.AccountType,
                 a.Balance,
                 a.Card?.CardNumber ?? "No Card", 
                 a.Card?.ExpiryDate ?? DateTime.MinValue
             )).ToList();
        
             return accountDtos;
         }

    }

}
