using BankSystemBackend.Enums;

namespace BankSystemBackend.Dto.AccountDTO
{
    public class CreateAccount
    {

        public string? CustomerFK { get; set; }
        public AccountType AccountType { get; set; }
    }
}
