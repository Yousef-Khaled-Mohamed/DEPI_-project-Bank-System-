//
using BankSystemBackend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BankSystemBackend.Configurations
{
    public class BranchConfiguration : IEntityTypeConfiguration<Branch>
    {
        public void Configure(EntityTypeBuilder<Branch> builder)
        {
            builder.HasKey(x => x.Id);
            builder.HasMany(x => x.Tellers).WithOne(x => x.Branch).HasForeignKey(x => x.BranchFK);

        }
    }
}
