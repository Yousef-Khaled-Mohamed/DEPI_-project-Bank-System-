using BankSystemBackend.Enums;

using System.ComponentModel.DataAnnotations;

namespace BankSystemBackend.Models
{
    public class BankTransaction
    {
        [Key]
        public int Id { get; set; }
        public int AccountId { get; set; }
        public string Message { get; set; } = string.Empty;
        public Account Account { get; set; } = null!;

        public decimal Amount { get; set; }
        public TransactionType Type { get; set; }
        public DateTime Date { get; set; }
        public int? TargetAccountId { get; set; }

        public int? TellerId { get; set; }
        public Teller? Teller { get; set; }
    }
}

