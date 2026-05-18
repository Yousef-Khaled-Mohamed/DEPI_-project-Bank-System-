using BankSystemBackend.Dto.BranchDto;
using BankSystemBackend.Dto.CustomerDTO;
using BankSystemBackend.Dto.TellerDTO;
using BankSystemBackend.IRepository;
using BankSystemBackend.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BankSystemBackend.Repository
{
    public class AdminRepo : IAdminRepo
    {
        private readonly AppDbContext _context;
        private readonly Microsoft.AspNetCore.Identity.UserManager<AppUser> _userManager;
        private static readonly Random _random = new();

        public AdminRepo(AppDbContext context, Microsoft.AspNetCore.Identity.UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // --- Realistic Card & Account Number Generators ------------------
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

        // --- Customer reads -------------------------------------------

        public async Task<List<DisplayCustomer>> GetAllCustomersAsync(string? search = null, string? status = null, int page = 1, int pageSize = 10)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;

            var query = _context.Customers.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var lower = search.ToLower();
                query = query.Where(c => (c.UserName != null && c.UserName.ToLower().Contains(lower)) 
                                      || (c.Email != null && c.Email.ToLower().Contains(lower))
                                      || (c.PhoneNumber != null && c.PhoneNumber.Contains(search)));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(c => c.Status == status);
            }

            var list = await query
                .OrderByDescending(c => c.CreatedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return list.Select(c => MapCustomer(c)).ToList();
        }

        public async Task<DisplayCustomer?> GetCustomerByIdAsync(int id)
        {
            var c = await _context.Customers.FindAsync(id);
            return c is null ? null : MapCustomer(c);
        }

        public async Task<int> GetCustomerCountAsync(string? search = null, string? status = null)
        {
            var query = _context.Customers.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var lower = search.ToLower();
                query = query.Where(c => (c.UserName != null && c.UserName.ToLower().Contains(lower)) 
                                      || (c.Email != null && c.Email.ToLower().Contains(lower))
                                      || (c.PhoneNumber != null && c.PhoneNumber.Contains(search)));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(c => c.Status == status);
            }

            return await query.CountAsync();
        }

        // --- Customer write -------------------------------------------
        public async Task<BankSystemBackend.Dto.AccountDTO.DisplayAccount> AddAccountAsync(int customerId, BankSystemBackend.Dto.AccountDTO.CreateAccount dto)
        {
            var customer = await _context.Customers.FindAsync(customerId)
                ?? throw new KeyNotFoundException($"Customer {customerId} not found.");

            // Under Bank Account System rules:
            // "Prevent duplicate accounts of the same type."
            var exists = await _context.Accounts.AnyAsync(a => a.CustomerId == customerId && a.AccountType == dto.AccountType);
            if (exists)
            {
                throw new InvalidOperationException($"Customer already has a {dto.AccountType} account.");
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

            return new BankSystemBackend.Dto.AccountDTO.DisplayAccount
            {
                Id = account.Id,
                CustomerId = account.CustomerId,
                AccountType = account.AccountType,
                AccountNumber = account.AccountNumber,
                Currency = account.Currency,
                AccountStatus = account.AccountStatus,
                Balance = account.Balance,
                CreatedDate = account.CreatedDate,
                Card = new BankSystemBackend.Dto.AccountDTO.DisplayCard
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

        public async Task<DisplayCustomer> AddCustomerAsync(AddCustomer dto)
        {
            var customer = new Customer
            {
                UserName = dto.Name,
                Email = dto.Email,
                PhotoUrl = dto.PhotoUrl,
                PhoneNumber = dto.PhoneNumber,
                City = dto.City,
                Street = dto.Street,
                State = dto.State,
                PostalCode = dto.PostalCode,
                Role = Enums.UserRole.Customer,
                Status = "Active",
                CreatedDate = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(customer, dto.Password);
            if (!result.Succeeded) throw new Exception(string.Join(", ", result.Errors.Select(e => e.Description)));
            await _userManager.AddToRoleAsync(customer, "Customer");

            return MapCustomer(customer);
        }

        public async Task<DisplayCustomer> UpdateCustomerAsync(int id, EditCustomer dto)
        {
            var customer = await _context.Customers.FindAsync(id)
                ?? throw new KeyNotFoundException($"Customer {id} not found.");

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

        public async Task<bool> DeleteCustomerAsync(int id)
        {
            var customer = await _userManager.FindByIdAsync(id.ToString());
            if (customer is null) return false;
            var result = await _userManager.DeleteAsync(customer);
            return result.Succeeded;
        }

        // --- Aggregate statistics -------------------------------------

        public async Task<decimal> GetTotalDepositsAsync()
        {
            return await _context.Transactions
                .Where(t => t.Type == Enums.TransactionType.Deposit
                         && t.Status == Enums.TransactionStatus.Completed)
                .SumAsync(t => t.Amount);
        }

        public async Task<decimal> GetTotalLoansAsync()
        {
            return await _context.Loans
                .Where(l => l.Status == Enums.LoanStatus.Approved)
                .SumAsync(l => (decimal)l.OriginalAmount);
        }

        public async Task<decimal> TotalAmountAsync()
        {
            return await _context.Accounts.SumAsync(a => a.Balance);
        }

        public async Task<decimal> GetTotalFeesAsync()
        {
            var totalTransfers = await _context.Transactions
                .Where(t => t.Type == Enums.TransactionType.Transfer
                         && t.Status == Enums.TransactionStatus.Completed)
                .SumAsync(t => t.Amount);

            return totalTransfers * 0.01m;
        }

        public async Task<decimal> TotalWithdrawalsAsync()
        {
            return await _context.Transactions
                .Where(t => t.Type == Enums.TransactionType.Withdraw
                         && t.Status == Enums.TransactionStatus.Completed)
                .SumAsync(t => t.Amount);
        }

        public async Task<int> GetTotalAccountsCountAsync()
        {
            return await _context.Accounts.CountAsync();
        }

        public async Task<int> GetTotalCardsCountAsync()
        {
            return await _context.BankCards.CountAsync();
        }

        // --- Teller CRUD ----------------------------------------------

        public async Task<DisplayTeller?> GetTellerByIdAsync(int id)
        {
            var t = await _context.Tellers.FindAsync(id);
            return t is null ? null : MapTeller(t);
        }

        public async Task<List<DisplayTeller>> GetAllTellersAsync(string? search = null, string? status = null, int page = 1, int pageSize = 10)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;

            var query = _context.Tellers.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var lower = search.ToLower();
                query = query.Where(t => (t.UserName != null && t.UserName.ToLower().Contains(lower)) 
                                      || (t.Email != null && t.Email.ToLower().Contains(lower))
                                      || (t.PhoneNumber != null && t.PhoneNumber.Contains(search)));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(t => t.Status == status);
            }

            var list = await query
                .OrderByDescending(t => t.CreatedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return list.Select(t => MapTeller(t)).ToList();
        }

        public async Task<int> GetTellerCountAsync(string? search = null, string? status = null)
        {
            var query = _context.Tellers.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var lower = search.ToLower();
                query = query.Where(t => (t.UserName != null && t.UserName.ToLower().Contains(lower)) 
                                      || (t.Email != null && t.Email.ToLower().Contains(lower))
                                      || (t.PhoneNumber != null && t.PhoneNumber.Contains(search)));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(t => t.Status == status);
            }

            return await query.CountAsync();
        }

        public async Task<DisplayTeller> AddTellerAsync(AddTeller dto)
        {
            var teller = new Teller
            {
                UserName = dto.Name,
                Email = dto.Email,
                PhotoUrl = dto.PhotoUrl,
                PhoneNumber = dto.PhoneNumber,
                BranchFK = dto.BranchFK,
                Role = Enums.UserRole.Teller,
                Status = "Active",
                CreatedDate = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(teller, dto.Password);
            if (!result.Succeeded) throw new Exception(string.Join(", ", result.Errors.Select(e => e.Description)));
            await _userManager.AddToRoleAsync(teller, "Teller");

            return MapTeller(teller);
        }

        public async Task<DisplayTeller> UpdateTellerAsync(int id, EditTeller dto)
        {
            var teller = await _context.Tellers.FindAsync(id)
                ?? throw new KeyNotFoundException($"Teller {id} not found.");

            teller.UserName = dto.Name;
            teller.Email = dto.Email;
            teller.PhotoUrl = dto.PhotoUrl;
            teller.BranchFK = dto.BranchFK;

            if (!string.IsNullOrWhiteSpace(dto.Password))
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(teller);
                var result = await _userManager.ResetPasswordAsync(teller, token, dto.Password);
                if (!result.Succeeded)
                    throw new Exception(string.Join(", ", result.Errors.Select(e => e.Description)));
            }

            await _context.SaveChangesAsync();
            return MapTeller(teller);
        }

        public async Task<bool> DeleteTellerAsync(int id)
        {
            var teller = await _userManager.FindByIdAsync(id.ToString());
            if (teller is null) return false;
            var result = await _userManager.DeleteAsync(teller);
            return result.Succeeded;
        }

        // --- Branch CRUD ----------------------------------------------

        public async Task<DisplayBranch> AddBranchAsync(AddBranch dto)
        {
            var branch = new Branch
            {
                Name = dto.Name,
                Location = dto.Address
            };

            _context.Branches.Add(branch);
            await _context.SaveChangesAsync();

            return new DisplayBranch { Id = branch.Id, Name = branch.Name, Address = branch.Location };
        }

        public async Task<DisplayBranch> UpdateBranchAsync(int id, EditBranch dto)
        {
            var branch = await _context.Branches.FindAsync(id)
                ?? throw new KeyNotFoundException($"Branch {id} not found.");

            branch.Name = dto.Name;
            branch.Location = dto.Address;

            await _context.SaveChangesAsync();
            return new DisplayBranch { Id = branch.Id, Name = branch.Name, Address = branch.Location };
        }

        public async Task<bool> DeleteBranchAsync(int id)
        {
            var branch = await _context.Branches.FindAsync(id);
            if (branch is null) return false;

            _context.Branches.Remove(branch);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<DisplayBranch>> GetAllBranchesAsync()
        {
            return await _context.Branches
                .Select(b => new DisplayBranch { Id = b.Id, Name = b.Name, Address = b.Location })
                .ToListAsync();
        }

        public async Task<DisplayBranch?> GetBranchByIdAsync(int id)
        {
            var b = await _context.Branches.FindAsync(id);
            return b is null ? null : new DisplayBranch { Id = b.Id, Name = b.Name, Address = b.Location };
        }

        public async Task<int> GetBranchCountAsync()
        {
            return await _context.Branches.CountAsync();
        }

        // --- Mapping helpers ------------------------------------------

        private static DisplayCustomer MapCustomer(Customer c) => new()
        {
            Id = c.Id,
            Name = c.UserName ?? string.Empty,
            Email = c.Email ?? string.Empty,
            PhotoUrl = c.PhotoUrl ?? string.Empty,
            PhoneNumber = c.PhoneNumber ?? string.Empty,
            City = c.City ?? string.Empty,
            Street = c.Street ?? string.Empty,
            State = c.State ?? string.Empty,
            PostalCode = c.PostalCode,
            Status = c.Status ?? "Active",
            CreatedDate = c.CreatedDate
        };

        private static DisplayTeller MapTeller(Teller t) => new()
        {
            Id = t.Id,
            Name = t.UserName ?? string.Empty,
            Email = t.Email ?? string.Empty,
            PhotoUrl = t.PhotoUrl ?? string.Empty,
            BranchFK = t.BranchFK,
            Status = t.Status ?? "Active",
            CreatedDate = t.CreatedDate
        };
    }
}
