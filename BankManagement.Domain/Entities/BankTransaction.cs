using BankManagement.Domain.Enum;
using System;
using System.Collections.Generic;
using System.Text;

namespace BankManagement.Domain.Entities
{
    public class BankTransaction
    {
        public int Id { get; set; }
        public int AccountId { get; set; }
        public BankAccount Account { get; set; } = null!;
        public decimal Amount { get; set; }
        public TransactionType Type { get; set; }
        public DateTime Date { get; set; }
        public int? TargetAccountId { get; set; }
    }
}
