using BankSystemBackend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BankSystemBackend.DbContext.Configurations
{
    public class BankCardConfiguration : IEntityTypeConfiguration<BankCard>
    {
        public void Configure(EntityTypeBuilder<BankCard> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.CardHolderName)
                   .IsRequired()
                   .HasMaxLength(100);

            builder.Property(x => x.CardNumber)
                   .IsRequired()
                   .HasMaxLength(20);

            builder.Property(x => x.CVV)
                   .IsRequired()
                   .HasMaxLength(4);

            builder.Property(x => x.ExpiryDate)
                   .IsRequired()
                   .HasMaxLength(7);

            builder.Property(x => x.CardType)
                   .IsRequired()
                   .HasMaxLength(20)
                   .HasDefaultValue("Visa");

            builder.Property(x => x.IBAN)
                   .IsRequired()
                   .HasMaxLength(34);

            builder.Property(x => x.CreatedDate)
                   .HasDefaultValueSql("GETUTCDATE()");
        }
    }
}
