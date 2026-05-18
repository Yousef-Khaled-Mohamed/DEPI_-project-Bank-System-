using BankSystemBackend.Enums;
using System;
using System.Collections.Generic;

namespace BankSystemBackend.Models
{
    public class Account
    {
        public int Id { get; set; }

        public string AccountNumber { get; set; } = string.Empty;

        public string Currency { get; set; } = "USD";

        public decimal Balance { get; set; }

        public AccountType AccountType { get; set; }

        public AccountStatus AccountStatus { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public ICollection<Transactions> Transactions { get; set; } = new List<Transactions>();

        public Customer Customer { get; set; } = null!;

        public int? CustomerId { get; set; }

        // Navigation property for 1-to-1 relation
        public BankCard? Card { get; set; }
    }
}
