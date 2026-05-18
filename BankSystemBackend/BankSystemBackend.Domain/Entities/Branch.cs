namespace BankSystemBackend.Models
{
    public class Branch
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Location { get; set; }
        public ICollection<Teller> Tellers { get; set; }


    }
}
