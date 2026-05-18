

namespace BankSystemBackend.Models
{
    public class Teller : AppUser
    {
        public ICollection<Transactions> Transactions { get; set; }
        public Branch Branch { get; set; }
        public int? BranchFK { get; set; }
    }
}
