using BankManagement.Application;
using BankManagement.Application.DTO;
using BankManagement.Application.IService;
using BankManagement.Domain.Entities;
using BankManagement.Domain.Enum;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace BankManagement.Infrastructure.Services
{
    public class CustomerService(
     AppDbContext dbContext,
     ILogger<CustomerService> logger) : ICustomerService
    {
        public async Task<IEnumerable<AccountDto>> GetAccountsAsync(string customerUserId)
        {
            return await dbContext.Accounts.Include(x => x.Card).Include(x => x.CustomerProfile)
                .Where(x => x.CustomerProfile.IdentityUserId == customerUserId)
                .Select(x => new AccountDto(x.Id, x.AccountNumber, x.AccountType, x.Balance, x.Card.CardNumber, x.Card.ExpiryDate))
                .ToListAsync();
        }

        public async Task<IEnumerable<TransactionDto>> GetTransactionsAsync(string customerUserId)
        {
            return await dbContext.Transactions.Include(x => x.Account).ThenInclude(x => x.CustomerProfile)
                .Where(x => x.Account.CustomerProfile.IdentityUserId == customerUserId)
                .OrderByDescending(x => x.Date)
                .Select(x => new TransactionDto(x.Id, x.AccountId, x.Amount, x.Type, x.Date, x.TargetAccountId))
                .ToListAsync();
        }

        public async Task<IEnumerable<LoanDto>> GetLoansAsync(string customerUserId)
        {
            return await dbContext.Loans.Include(x => x.CustomerProfile)
                .Where(x => x.CustomerProfile.IdentityUserId == customerUserId)
                .Select(x => new LoanDto(x.Id, x.Amount, x.InterestRate, x.DurationMonths, x.CustomerProfileId))
                .ToListAsync();
        }

        public async Task<TransactionDto> TransferAsync(string customerUserId, TransferDto request)
        {
            var source = await dbContext.Accounts.Include(x => x.CustomerProfile)
                .FirstOrDefaultAsync(x => x.Id == request.SourceAccountId && x.CustomerProfile.IdentityUserId == customerUserId)
                ?? throw new UnauthorizedAccessException("Source account does not belong to customer.");

            var target = await dbContext.Accounts.FirstOrDefaultAsync(x => x.Id == request.TargetAccountId)
                ?? throw new KeyNotFoundException("Target account not found.");

            if (source.Balance < request.Amount) throw new InvalidOperationException("Insufficient balance.");

            await using var tx = await dbContext.Database.BeginTransactionAsync();
            source.Balance -= request.Amount;
            target.Balance += request.Amount;
            var transferRecord = new BankTransaction
            {
                AccountId = source.Id,
                Amount = request.Amount,
                Type = TransactionType.Transfer,
                Date = DateTime.UtcNow,
                TargetAccountId = target.Id
            };
            dbContext.Transactions.Add(transferRecord);
            await dbContext.SaveChangesAsync();
            await tx.CommitAsync();

            logger.LogInformation("Transfer completed. Customer:{CustomerId} From:{Source} To:{Target} Amount:{Amount}",
                customerUserId, source.Id, target.Id, request.Amount);

            return new TransactionDto(transferRecord.Id, transferRecord.AccountId, transferRecord.Amount, transferRecord.Type, transferRecord.Date, transferRecord.TargetAccountId);
        }
    }

}
