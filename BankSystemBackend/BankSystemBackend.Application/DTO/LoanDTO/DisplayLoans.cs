using BankSystemBackend.Enums;

namespace BankSystemBackend.Dto.LoanDTO
{
    public class DisplayLoans
    {
        public int Id { get; set; }
        public decimal OriginalAmount { get; set; }
        public decimal Amount { get; set; }
        public decimal InterestRate { get; set; }
        public int DurationMonths { get; set; }
        public DateTime StartDate { get; set; }
        public LoanStatus Status { get; set; }
        public int? CustomerId { get; set; }
    }
}

