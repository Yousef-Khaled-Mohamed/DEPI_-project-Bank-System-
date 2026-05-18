using BankSystemBackend.Enums;
using BankSystemBackend.Models;

namespace BankSystemBackend
{
    public class Loan
    {
        public int Id { get; set; }
        public double OriginalAmount { get; set; }
        public double RemainingAmount { get; set; }
        public double InterestRate { get; set; }
        public int DurationMonths { get; set; }
        public string Purpose { get; set; }
        public LoanStatus Status { get; set; }
        public DateTime StartDate { get; set; }
        public Customer Customer { get; set; }
        public int? CustomerId { get; set; }


    }
}

