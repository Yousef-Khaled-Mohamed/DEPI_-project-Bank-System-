using BankSystemBackend.Enums;

namespace BankSystemBackend
{
    public class TransactionResponseDto
    {
        public int Id { get; set; }
        public int AccountId { get; set; }
        public string Message { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public TransactionType Type { get; set; }
        public DateTime Date { get; set; }
        public int? TargetAccountId { get; set; }
    }
}


