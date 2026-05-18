

namespace BankSystemBackend.Models
{
    public class Customer : AppUser
    {

        public string City { get; set; }
        public string Street { get; set; }
        public string State { get; set; }
        public int PostalCode { get; set; }

        public ICollection<Account> Accounts { get; set; }
        public ICollection<Loan> Loans { get; set; }








    }
}
