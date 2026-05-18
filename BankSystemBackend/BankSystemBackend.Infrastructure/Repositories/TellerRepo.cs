using BankSystemBackend.Dto.AccountDTO;
using BankSystemBackend.Dto.CustomerDTO;
using BankSystemBackend.Dto.LoanDTO;
using BankSystemBackend.Enums;
using BankSystemBackend.IRepository;
using BankSystemBackend.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BankSystemBackend.Repository
{
    public class TellerRepo : ITellerRepo
    {
        private readonly AppDbContext _context;

        public TellerRepo(AppDbContext context)
        {
            _context = context;
        }

        // --- Transactions ---------------------------------------------

        public async Task<TransactionResponseDto> DepositAsync(int customerId, BankSystemBackend.Enums.AccountType accountType, decimal amount, string message = "", int? tellerId = null)
        {
            if (amount <= 0)
                throw new InvalidOperationException("Deposit amount must be greater than zero.");

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var customer = await _context.Customers.FindAsync(customerId)
                    ?? throw new KeyNotFoundException($"Customer with ID {customerId} not found.");

                var account = await _context.Accounts.FirstOrDefaultAsync(a => a.CustomerId == customerId && a.AccountType == accountType)
                    ?? throw new KeyNotFoundException($"Account of type {accountType} not found for customer {customerId}.");

                account.Balance += amount;
                var bankTransaction = new Transactions
                {
                    Date = DateTime.UtcNow, Amount = amount, Message = message,
                    Type = TransactionType.Deposit, Status = TransactionStatus.Completed,
                    AccountFK = account.Id, TellerFK = tellerId
                };
                _context.Transactions.Add(bankTransaction);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return new TransactionResponseDto
                {
                    Id = bankTransaction.Id, AccountId = account.Id,
                    Amount = amount, Message = message,
                    Type = TransactionType.Deposit,
                    Date = bankTransaction.Date
                };
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<TransactionResponseDto> WithdrawAsync(int customerId, BankSystemBackend.Enums.AccountType accountType, decimal amount, string message = "", int? tellerId = null)
        {
            if (amount <= 0)
                throw new InvalidOperationException("Withdrawal amount must be greater than zero.");

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var customer = await _context.Customers.FindAsync(customerId)
                    ?? throw new KeyNotFoundException($"Customer with ID {customerId} not found.");

                var account = await _context.Accounts.FirstOrDefaultAsync(a => a.CustomerId == customerId && a.AccountType == accountType)
                    ?? throw new KeyNotFoundException($"Account of type {accountType} not found for customer {customerId}.");

                if (account.Balance < amount)
                    throw new InvalidOperationException("Insufficient balance.");

                account.Balance -= amount;
                var bankTransaction = new Transactions
                {
                    Date = DateTime.UtcNow, Amount = amount, Message = message,
                    Type = TransactionType.Withdraw, Status = TransactionStatus.Completed,
                    AccountFK = account.Id, TellerFK = tellerId
                };
                _context.Transactions.Add(bankTransaction);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return new TransactionResponseDto
                {
                    Id = bankTransaction.Id, AccountId = account.Id,
                    Amount = amount, Message = message,
                    Type = TransactionType.Withdraw,
                    Date = bankTransaction.Date
                };
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<TransactionResponseDto> TransferAsync(TransactionDto dto, int? tellerId = null)
        {
            if (dto.Amount <= 0)
                throw new InvalidOperationException("Transfer amount must be greater than zero.");

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var sourceAccount = await _context.Accounts.FindAsync(dto.AccountId)
                    ?? throw new KeyNotFoundException($"Account {dto.AccountId} not found.");
                    
                Account? targetAccount = null;
                if (dto.TargetAccountId.HasValue)
                {
                    targetAccount = await _context.Accounts.FindAsync(dto.TargetAccountId.Value);
                }
                else if (dto.TargetCustomerId.HasValue && dto.TargetAccountType.HasValue)
                {
                    targetAccount = await _context.Accounts.FirstOrDefaultAsync(a => a.CustomerId == dto.TargetCustomerId.Value && a.AccountType == dto.TargetAccountType.Value);
                }

                if (targetAccount == null)
                    throw new KeyNotFoundException($"Target account not found.");
                    
                if (sourceAccount.Id == targetAccount.Id)
                    throw new InvalidOperationException("Cannot transfer to the same account.");
                if (sourceAccount.CustomerId == targetAccount.CustomerId)
                    throw new InvalidOperationException("Cannot transfer to the same customer.");
                if (sourceAccount.Balance < dto.Amount)
                    throw new InvalidOperationException("Insufficient balance.");
                sourceAccount.Balance -= dto.Amount;
                targetAccount.Balance += dto.Amount;

                // Primary record on source account
                var bankTransaction = new Transactions
                {
                    Date = DateTime.UtcNow, Amount = dto.Amount, Message = dto.Message,
                    Type = TransactionType.Transfer, Status = TransactionStatus.Completed,
                    AccountFK = dto.AccountId, TargetAccountFK = targetAccount.Id,
                    TellerFK = tellerId
                };
                _context.Transactions.Add(bankTransaction);

                // Mirror record on target account so its history is visible
                var mirrorTransaction = new Transactions
                {
                    Date = DateTime.UtcNow, Amount = dto.Amount, Message = dto.Message,
                    Type = TransactionType.Transfer, Status = TransactionStatus.Completed,
                    AccountFK = targetAccount.Id, TargetAccountFK = dto.AccountId,
                    TellerFK = tellerId
                };
                _context.Transactions.Add(mirrorTransaction);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return MapTransaction(bankTransaction, dto.AccountId, targetAccount.Id);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<List<TransactionResponseDto>> GetTransactionHistoryAsync(int accountId, int page = 1, int pageSize = 10)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;
            return await _context.Transactions
                .Where(t => t.AccountFK == accountId)
                .OrderByDescending(t => t.Date)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(t => new TransactionResponseDto
                {
                    Id = t.Id,
                    AccountId = accountId,
                    Amount = t.Amount,
                    Message = t.Message,
                    Type = t.Type,
                    Date = t.Date,
                    TargetAccountId = t.TargetAccountFK
                })
                .ToListAsync();
        }

        public async Task<TransactionResponseDto?> GetTransactionByIdAsync(int transactionId)
        {
            var t = await _context.Transactions.FindAsync(transactionId);
            return t is null ? null : MapTransaction(t, t.AccountFK ?? 0);
        }

        // --- Accounts -------------------------------------------------

        public async Task<DisplayAccount?> GetAccountByIdAsync(int accountId)
        {
            var a = await _context.Accounts.Include(x => x.Card).FirstOrDefaultAsync(x => x.Id == accountId);
            return a is null ? null : MapAccount(a);
        }

        public async Task<List<DisplayAccount>> GetCustomerAccountsAsync(int customerId, int page = 1, int pageSize = 10)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;
            return await _context.Accounts
                .Include(a => a.Card)
                .Where(a => a.CustomerId == customerId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(a => MapAccount(a))
                .ToListAsync();
        }

        public async Task<decimal> GetAccountBalanceAsync(int accountId)
        {
            var account = await _context.Accounts.FindAsync(accountId)
                ?? throw new KeyNotFoundException($"Account {accountId} not found.");
            return account.Balance;
        }

        // --- Customer & Loans -----------------------------------------

        public async Task<DisplayLoans> AddLoansAsync(int customerId, AddLoan dto, int? tellerId = null)
        {
            if (dto.OriginalAmount <= 0)
                throw new InvalidOperationException("Loan amount must be greater than zero.");

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var customer = await _context.Customers.FindAsync(customerId)
                    ?? throw new KeyNotFoundException($"Customer {customerId} not found.");
                var loan = new Loan
                {
                    OriginalAmount = (double)dto.OriginalAmount,
                    RemainingAmount = (double)dto.OriginalAmount,
                    InterestRate = (double)dto.InterestRate,
                    DurationMonths = dto.DurationMonths,
                    Purpose = dto.Purpose,
                    Status = dto.Status,
                    StartDate = dto.StartDate,
                    CustomerId = customerId
                };
                _context.Loans.Add(loan);

                // If approved, disburse the loan to customer's account and create a transaction record
                if (dto.Status == LoanStatus.Approved)
                {
                    // Find customer's Current account first, else Savings account
                    var account = await _context.Accounts.FirstOrDefaultAsync(a => a.CustomerId == customerId && a.AccountType == AccountType.Current)
                        ?? await _context.Accounts.FirstOrDefaultAsync(a => a.CustomerId == customerId && a.AccountType == AccountType.Saving)
                        ?? throw new InvalidOperationException("Customer has no bank accounts to disburse the loan.");

                    account.Balance += dto.OriginalAmount;

                    // Create transaction record
                    var loanTransaction = new Transactions
                    {
                        Date = DateTime.UtcNow,
                        Amount = dto.OriginalAmount,
                        Message = $"Loan Disbursed: {dto.Purpose}",
                        Type = TransactionType.Loan,
                        Status = TransactionStatus.Completed,
                        AccountFK = account.Id,
                        TellerFK = tellerId
                    };
                    _context.Transactions.Add(loanTransaction);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return new DisplayLoans
                {
                    Id = loan.Id,
                    OriginalAmount = (decimal)loan.OriginalAmount,
                    Amount = (decimal)loan.RemainingAmount,
                    InterestRate = (decimal)loan.InterestRate,
                    DurationMonths = loan.DurationMonths,
                    StartDate = loan.StartDate,
                    Status = loan.Status,
                    CustomerId = loan.CustomerId
                };
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<DisplayCustomer?> GetCustomerByIdAsync(int customerId)
        {
            var c = await _context.Customers.FindAsync(customerId);
            return c is null ? null : MapCustomer(c);
        }

        public async Task<List<DisplayLoans>> GetCustomerLoansAsync(int customerId)
        {
            return await _context.Loans
                .Where(l => l.CustomerId == customerId)
                .Select(l => new DisplayLoans
                {
                    Id = l.Id,
                    OriginalAmount = (decimal)l.OriginalAmount,
                    Amount = (decimal)l.RemainingAmount,
                    InterestRate = (decimal)l.InterestRate,
                    DurationMonths = l.DurationMonths,
                    StartDate = l.StartDate,
                    Status = l.Status,
                    CustomerId = l.CustomerId
                })
                .ToListAsync();
        }

        // --- Mapping helpers ------------------------------------------

        private static DisplayCustomer MapCustomer(Customer c) => new()
        {
            Id = c.Id, Name = c.UserName ?? string.Empty, Email = c.Email ?? string.Empty, PhotoUrl = c.PhotoUrl ?? string.Empty,
            PhoneNumber = c.PhoneNumber ?? string.Empty, City = c.City ?? string.Empty, Street = c.Street ?? string.Empty,
            State = c.State ?? string.Empty, PostalCode = c.PostalCode, Status = c.Status ?? "Active", CreatedDate = c.CreatedDate
        };

        private static DisplayAccount MapAccount(Account a) => new()
        {
            Id = a.Id,
            CustomerId = a.CustomerId,
            AccountNumber = a.AccountNumber,
            Currency = a.Currency,
            AccountType = a.AccountType,
            AccountStatus = a.AccountStatus,
            Balance = a.Balance,
            CreatedDate = a.CreatedDate,
            Card = a.Card == null ? null : new DisplayCard
            {
                Id = a.Card.Id,
                CardHolderName = a.Card.CardHolderName,
                CardNumber = a.Card.CardNumber,
                CVV = a.Card.CVV,
                ExpiryDate = a.Card.ExpiryDate,
                CardType = a.Card.CardType,
                IBAN = a.Card.IBAN
            }
        };

        private static TransactionResponseDto MapTransaction(
            Transactions t, int accountId, int? targetAccountId = null) => new()
            {
                Id = t.Id, AccountId = accountId, Amount = t.Amount, Message = t.Message,
                Type = t.Type, Date = t.Date, TargetAccountId = targetAccountId ?? t.TargetAccountFK
            };
    }
}
