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
    public class CustomerRepo : ICustomerRepo
    {
        private readonly AppDbContext _context;
        private readonly Microsoft.AspNetCore.Identity.UserManager<AppUser> _userManager;
        private static readonly Random _random = new();

        public CustomerRepo(AppDbContext context, Microsoft.AspNetCore.Identity.UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private static string GenerateRandomDigits(int length)
        {
            var chars = new char[length];
            for (int i = 0; i < length; i++)
            {
                chars[i] = (char)('0' + _random.Next(10));
            }
            return new string(chars);
        }

        private static string GenerateIBAN(string accountNumber)
        {
            return $"US{_random.Next(10, 99)}BKNK{accountNumber}";
        }

        private static string GenerateCardNumber(string cardType)
        {
            var prefix = cardType.Equals("MasterCard", StringComparison.OrdinalIgnoreCase) ? "51" : "41";
            var remainingDigits = GenerateRandomDigits(14);
            var raw = prefix + remainingDigits;
            return $"{raw.Substring(0, 4)} {raw.Substring(4, 4)} {raw.Substring(8, 4)} {raw.Substring(12, 4)}";
        }

        public async Task<TransactionResponseDto> TransferAsync(TransactionDto dto)
        {
            if (dto.Amount <= 0)
                throw new InvalidOperationException("Transfer amount must be greater than zero.");
            if (dto.AccountId == dto.TargetAccountId)
                throw new InvalidOperationException("Cannot transfer to the same account.");

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

                // Primary transfer record on source account
                var bankTransaction = new Transactions
                {
                    Date = DateTime.UtcNow,
                    Amount = dto.Amount,
                    Message = dto.Message,
                    Type = TransactionType.Transfer,
                    Status = TransactionStatus.Completed,
                    AccountFK = dto.AccountId,
                    TargetAccountFK = targetAccount.Id
                };
                _context.Transactions.Add(bankTransaction);

                // Mirror record on target account so its history shows the incoming transfer
                var mirrorTransaction = new Transactions
                {
                    Date = DateTime.UtcNow,
                    Amount = dto.Amount,
                    Message = dto.Message,
                    Type = TransactionType.Transfer,
                    Status = TransactionStatus.Completed,
                    AccountFK = targetAccount.Id,
                    TargetAccountFK = dto.AccountId
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

        public async Task<List<TransactionResponseDto>> GetAllTransactionsAsync(int accountId, int page = 1, int pageSize = 10)
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

        public async Task<DisplayCustomer?> GetProfileAsync(int customerId)
        {
            var c = await _context.Customers.FindAsync(customerId);
            return c is null ? null : MapCustomer(c);
        }

        public async Task<DisplayCustomer> UpdateProfileAsync(int customerId, EditCustomer dto)
        {
            var customer = await _context.Customers.FindAsync(customerId)
                ?? throw new KeyNotFoundException($"Customer {customerId} not found.");
            customer.UserName = dto.Name;
            customer.Email = dto.Email;
            customer.PhotoUrl = dto.PhotoUrl;
            customer.PhoneNumber = dto.PhoneNumber;
            customer.City = dto.City;
            customer.Street = dto.Street;
            customer.State = dto.State;
            customer.PostalCode = dto.PostalCode;
            if (!string.IsNullOrWhiteSpace(dto.Password))
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(customer);
                var result = await _userManager.ResetPasswordAsync(customer, token, dto.Password);
                if (!result.Succeeded)
                    throw new Exception(string.Join(", ", result.Errors.Select(e => e.Description)));
            }
            await _context.SaveChangesAsync();
            return MapCustomer(customer);
        }

        public async Task<Microsoft.AspNetCore.Identity.IdentityResult> ChangePasswordAsync(int customerId, ChangePasswordDto dto)
        {
            var customer = await _userManager.FindByIdAsync(customerId.ToString());
            if (customer is null) return Microsoft.AspNetCore.Identity.IdentityResult.Failed(new Microsoft.AspNetCore.Identity.IdentityError { Description = "User not found" });
            return await _userManager.ChangePasswordAsync(customer, dto.CurrentPassword, dto.NewPassword);
        }

        public async Task<List<DisplayAccount>> GetMyAccountsAsync(int customerId, int page = 1, int pageSize = 10)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;
            return await _context.Accounts
                .Include(a => a.Card)
                .Where(a => a.CustomerId == customerId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(a => new DisplayAccount
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
                })
                .ToListAsync();
        }

        public async Task<bool> OwnsAccountAsync(int customerId, int accountId)
        {
            return await _context.Accounts
                .AnyAsync(a => a.Id == accountId && a.CustomerId == customerId);
        }

        public async Task<decimal> GetAccountBalanceAsync(int accountId)
        {
            var account = await _context.Accounts.FindAsync(accountId)
                ?? throw new KeyNotFoundException($"Account {accountId} not found.");
            return account.Balance;
        }

        public async Task<DisplayAccount> CreateAccountForMeAsync(int customerId, CreateAccount dto)
        {
            var customer = await _context.Customers.FindAsync(customerId)
                ?? throw new KeyNotFoundException($"Customer {customerId} not found.");

            // Under Bank Account System rules:
            // "Prevent duplicate accounts of the same type."
            var exists = await _context.Accounts.AnyAsync(a => a.CustomerId == customerId && a.AccountType == dto.AccountType);
            if (exists)
            {
                throw new InvalidOperationException($"You already have a {dto.AccountType} account.");
            }

            var accountNumber = "200" + GenerateRandomDigits(7);
            var iban = GenerateIBAN(accountNumber);
            var cardType = _random.Next(2) == 0 ? "Visa" : "MasterCard";
            var cardNumber = GenerateCardNumber(cardType);
            var cvv = _random.Next(100, 999).ToString();
            var expiry = DateTime.UtcNow.AddYears(5).ToString("MM/yy");

            var account = new Account
            {
                CustomerId = customerId,
                AccountType = dto.AccountType,
                Balance = 0,
                AccountStatus = Enums.AccountStatus.Active,
                AccountNumber = accountNumber,
                Currency = "USD",
                CreatedDate = DateTime.UtcNow
            };

            _context.Accounts.Add(account);
            await _context.SaveChangesAsync();

            var card = new BankCard
            {
                AccountId = account.Id,
                CardHolderName = customer.UserName ?? "Valued Customer",
                CardNumber = cardNumber,
                CVV = cvv,
                ExpiryDate = expiry,
                CardType = cardType,
                IBAN = iban,
                CreatedDate = DateTime.UtcNow
            };

            _context.BankCards.Add(card);
            await _context.SaveChangesAsync();

            account.Card = card;

            return new DisplayAccount
            {
                Id = account.Id,
                CustomerId = account.CustomerId,
                AccountNumber = account.AccountNumber,
                Currency = account.Currency,
                AccountType = account.AccountType,
                AccountStatus = account.AccountStatus,
                Balance = account.Balance,
                CreatedDate = account.CreatedDate,
                Card = new DisplayCard
                {
                    Id = card.Id,
                    CardHolderName = card.CardHolderName,
                    CardNumber = card.CardNumber,
                    CVV = card.CVV,
                    ExpiryDate = card.ExpiryDate,
                    CardType = card.CardType,
                    IBAN = card.IBAN
                }
            };
        }

        public async Task<List<DisplayLoans>> GetMyLoansAsync(int customerId)
        {
            return await _context.Loans
                .Where(l => l.CustomerId == customerId)
                .Select(l => MapLoan(l))
                .ToListAsync();
        }

        public async Task<DisplayLoans?> GetLoanByIdAsync(int loanId)
        {
            var loan = await _context.Loans.FindAsync(loanId);
            return loan is null ? null : MapLoan(loan);
        }

        private static DisplayCustomer MapCustomer(Customer c) => new()
        {
            Id = c.Id, Name = c.UserName ?? string.Empty, Email = c.Email ?? string.Empty, PhotoUrl = c.PhotoUrl ?? string.Empty,
            PhoneNumber = c.PhoneNumber ?? string.Empty, City = c.City ?? string.Empty, Street = c.Street ?? string.Empty,
            State = c.State ?? string.Empty, PostalCode = c.PostalCode, Status = c.Status ?? "Active", CreatedDate = c.CreatedDate
        };

        private static DisplayLoans MapLoan(Loan l) => new()
        {
            Id = l.Id, OriginalAmount = (decimal)l.OriginalAmount,
            Amount = (decimal)l.RemainingAmount, InterestRate = (decimal)l.InterestRate,
            DurationMonths = l.DurationMonths, StartDate = l.StartDate,
            Status = l.Status, CustomerId = l.CustomerId
        };

        private static TransactionResponseDto MapTransaction(Transactions t, int accountId, int? targetAccountId = null) => new()
        {
            Id = t.Id, AccountId = accountId, Amount = t.Amount, Message = t.Message,
            Type = t.Type, Date = t.Date, TargetAccountId = targetAccountId ?? t.TargetAccountFK
        };
    }
}
