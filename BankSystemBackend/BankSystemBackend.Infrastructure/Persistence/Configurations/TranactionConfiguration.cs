
using BankSystemBackend.Enums;
using BankSystemBackend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;


namespace BankSystemBackend.Configurations
{
    public class TranactionConfiguration : IEntityTypeConfiguration<Transactions>
    {
        public void Configure(EntityTypeBuilder<Transactions> builder)
        {
            builder.HasKey(t => t.Id);
            builder.Property(t => t.Date).HasDefaultValueSql("GETDATE()");
            builder.Property(t => t.Amount).IsRequired();

            builder.Property(t => t.Status).HasDefaultValue(TransactionStatus.Pending);

            builder.HasOne(t => t.TargetAccount)
                   .WithMany()
                   .HasForeignKey(t => t.TargetAccountFK)
                   .OnDelete(DeleteBehavior.Restrict);

        }
    }
}
