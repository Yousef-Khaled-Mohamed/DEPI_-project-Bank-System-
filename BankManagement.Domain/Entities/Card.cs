using System;
using System.Collections.Generic;
using System.Text;

namespace BankManagement.Domain.Entities
{
    public class Card
    {
        public int Id { get; set; }
        public int AccountId { get; set; }
        public BankAccount Account { get; set; } = null!;
        public string CardNumber { get; set; } = string.Empty;
        public DateTime ExpiryDate { get; set; }
        public string Cvv { get; set; } = string.Empty;
    }
}
