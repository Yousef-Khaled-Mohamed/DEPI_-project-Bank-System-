
using BankSystemBackend.Enums;
using BankSystemBackend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BankSystemBackend.Configurations
{
    public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
    {
        public void Configure(EntityTypeBuilder<Customer> builder)
        {

            builder.Property(x => x.City).HasMaxLength(50);
            builder.Property(x => x.Street).HasMaxLength(100);
            builder.Property(x => x.State).HasMaxLength(50);


            builder.Property(x => x.Role).HasDefaultValue(UserRole.Customer);
            builder.HasMany(x => x.Loans).WithOne(x => x.Customer).HasForeignKey(x => x.CustomerId);
            builder.HasMany(x => x.Accounts).WithOne(x => x.Customer).HasForeignKey(x => x.CustomerId);




        }
    }
}
