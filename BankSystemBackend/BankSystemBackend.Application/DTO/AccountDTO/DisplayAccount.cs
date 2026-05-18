using BankSystemBackend.Enums;
using System;

namespace BankSystemBackend.Dto.AccountDTO
{
    public class DisplayAccount
    {
        public int Id { get; set; }
        public int? CustomerId { get; set; }
        public string AccountNumber { get; set; } = string.Empty;
        public string Currency { get; set; } = "USD";
        public AccountType AccountType { get; set; }
        public AccountStatus AccountStatus { get; set; }
        public decimal Balance { get; set; }
        public DateTime CreatedDate { get; set; }

        // Nested card details for premium 3D flip card rendering
        public DisplayCard? Card { get; set; }
    }
}
