using BankSystemBackend.Enums;
using BankSystemBackend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Balance)
               .HasDefaultValue(0)
               .HasColumnType("decimal(18,2)");

        builder.Property(x => x.AccountType)
               .HasDefaultValue(AccountType.None);

        builder.Property(x => x.AccountStatus)
               .HasDefaultValue(AccountStatus.None);

        builder.Property(x => x.AccountNumber)
               .IsRequired()
               .HasMaxLength(20);

        builder.Property(x => x.Currency)
               .IsRequired()
               .HasMaxLength(5)
               .HasDefaultValue("USD");

        builder.Property(x => x.CreatedDate)
               .HasDefaultValueSql("GETUTCDATE()");

        builder.HasMany(x => x.Transactions)
               .WithOne(x => x.account)
               .HasForeignKey(x => x.AccountFK);

        builder.HasOne(x => x.Customer)
               .WithMany(x => x.Accounts)
               .HasForeignKey(x => x.CustomerId)
               .OnDelete(DeleteBehavior.Restrict);

        // 1-to-1 relationship with BankCard
        builder.HasOne(x => x.Card)
               .WithOne(x => x.Account)
               .HasForeignKey<BankCard>(x => x.AccountId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
