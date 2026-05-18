using System;

namespace BankSystemBackend.Models
{
    public class BankCard
    {
        public int Id { get; set; }

        public string CardHolderName { get; set; } = string.Empty;

        public string CardNumber { get; set; } = string.Empty;

        public string CVV { get; set; } = string.Empty;

        public string ExpiryDate { get; set; } = string.Empty; // e.g. "12/30"

        public string CardType { get; set; } = "Visa"; // "Visa" or "MasterCard"

        public string IBAN { get; set; } = string.Empty;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        // Relation: 1-to-1 with Account
        public int AccountId { get; set; }
        public Account Account { get; set; } = null!;
    }
}
