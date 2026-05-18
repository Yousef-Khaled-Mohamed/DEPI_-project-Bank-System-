

using BankSystemBackend.Enums;
using BankSystemBackend.Models;

namespace BankSystemBackend
{
    public class TransactionDto
    {


        public int AccountId { get; set; }
        public string Message { get; set; } = string.Empty;
        
        public decimal Amount { get; set; }
        public TransactionType Type { get; set; }
        public DateTime Date { get; set; }
        
        // Original target (optional now)
        public int? TargetAccountId { get; set; }
        
        // New target fields
        public int? TargetCustomerId { get; set; }
        public BankSystemBackend.Enums.AccountType? TargetAccountType { get; set; }

    }
}


