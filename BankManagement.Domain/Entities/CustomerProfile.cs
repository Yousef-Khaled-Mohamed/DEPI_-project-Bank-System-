using System;
using System.Collections.Generic;
using System.Text;

namespace BankManagement.Domain.Entities
{
    public class CustomerProfile
    {
        public int Id { get; set; }
        public string IdentityUserId { get; set; } = string.Empty;
        public string CreatedByTellerId { get; set; } = string.Empty;
        public ICollection<BankAccount> Accounts { get; set; } = new List<BankAccount>();
        public ICollection<Loan> Loans { get; set; } = new List<Loan>();
    }
}
