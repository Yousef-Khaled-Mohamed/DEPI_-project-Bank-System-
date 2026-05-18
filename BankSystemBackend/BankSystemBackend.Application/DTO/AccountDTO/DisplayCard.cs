using System;

namespace BankSystemBackend.Dto.AccountDTO
{
    public class DisplayCard
    {
        public int Id { get; set; }
        public string CardHolderName { get; set; } = string.Empty;
        public string CardNumber { get; set; } = string.Empty;
        public string CVV { get; set; } = string.Empty;
        public string ExpiryDate { get; set; } = string.Empty;
        public string CardType { get; set; } = string.Empty; // "Visa" or "MasterCard"
        public string IBAN { get; set; } = string.Empty;
    }
}
