using BankManagement.Domain.Enum;
using System;
using System.Collections.Generic;
using System.Text;

namespace BankManagement.Domain.Entities
{
    public class BankAccount
    {
        public int Id { get; set; }
        public string AccountNumber { get; set; } = string.Empty;
        public AccountType AccountType { get; set; }
        public decimal Balance { get; set; }
        public int CustomerProfileId { get; set; }
        public CustomerProfile CustomerProfile { get; set; } = null!;
        public Card Card { get; set; } = null!;
        public ICollection<BankTransaction> Transactions { get; set; } = new List<BankTransaction>();
    }
}
