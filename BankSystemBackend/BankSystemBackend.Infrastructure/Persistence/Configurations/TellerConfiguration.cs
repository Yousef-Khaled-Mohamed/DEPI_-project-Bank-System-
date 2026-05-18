
using BankSystemBackend.Enums;
using BankSystemBackend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BankSystemBackend.Configurations
{
    public class TellerConfiguration : IEntityTypeConfiguration<Teller>
    {
        public void Configure(EntityTypeBuilder<Teller> builder)
        {

            builder.Property(x => x.Role).HasDefaultValue(UserRole.Teller);
            builder.HasMany(x => x.Transactions).WithOne(x => x.Tellers).HasForeignKey(x => x.TellerFK);
        }
    }
}
