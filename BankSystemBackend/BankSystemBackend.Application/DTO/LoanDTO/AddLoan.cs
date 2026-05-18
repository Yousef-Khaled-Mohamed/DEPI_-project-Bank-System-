using BankSystemBackend.Enums;

namespace BankSystemBackend.Dto.LoanDTO
{
    public class AddLoan
    {
        public decimal OriginalAmount { get; set; }
        public decimal Amount { get; set; }
        public decimal InterestRate { get; set; }
        public int DurationMonths { get; set; }
        public DateTime StartDate { get; set; } = DateTime.UtcNow;
        public LoanStatus Status { get; set; }
        public int? CustomerId { get; set; }
        public string Purpose { get; set; } = string.Empty;
    }
}


