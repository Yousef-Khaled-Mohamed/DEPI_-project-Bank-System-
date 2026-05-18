namespace BankSystemBackend.Dto.CustomerDTO
{
    public class AddCustomer
    {

        public string Name { get; set; }
        public string PhoneNumber { get; set; }
        public string City { get; set; }
        public string Street { get; set; }
        public string State { get; set; }
        public int PostalCode { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string? PhotoUrl { get; set; }
    }
}
