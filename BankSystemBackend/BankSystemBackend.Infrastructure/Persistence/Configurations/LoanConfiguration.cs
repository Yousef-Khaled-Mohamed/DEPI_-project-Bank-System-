
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BankSystemBackend.Configurations
{
    public class LoanConfiguration
    {
        public void Configure(EntityTypeBuilder<Loan> builder)
        {
            builder.HasKey(x => x.Id);

        }
    }
}
