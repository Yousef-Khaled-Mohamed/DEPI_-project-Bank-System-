
using BankSystemBackend.Enums;


namespace BankSystemBackend.Models
{
    public class Transactions
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public decimal Amount { get; set; }
        public string? Message { get; set; }
        public TransactionType Type { get; set; }
        public TransactionStatus Status { get; set; }
        public Teller? Tellers { get; set; }
        public int? TellerFK { get; set; }
        public int? AccountFK { get; set; }
        public Account? account { get; set; }
        public int? TargetAccountFK { get; set; }
        public Account? TargetAccount { get; set; }


    }
}
