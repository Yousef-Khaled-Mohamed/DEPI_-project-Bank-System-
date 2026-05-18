using System;

namespace BankSystemBackend.Dto.TellerDTO
{
    public class DisplayTeller
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Street { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public int PostalCode { get; set; }
        public string Email { get; set; } = string.Empty;
        public string PhotoUrl { get; set; } = string.Empty;
        public int? BranchFK { get; set; }
        public string Status { get; set; } = "Active";
        public DateTime CreatedDate { get; set; }
    }
}
