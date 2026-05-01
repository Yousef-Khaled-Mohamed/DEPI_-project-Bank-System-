using BankManagement.Application;
using BankManagement.Application.IService;
using BankManagement.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace BankManagement.Infrastructure.Services
{
    public class ApplicationUser : IdentityUser
    {
    }

    public class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<ApplicationUser>(options)
    {
        public DbSet<Branch> Branches => Set<Branch>();
        public DbSet<CustomerProfile> CustomerProfiles => Set<CustomerProfile>();
        public DbSet<BankAccount> Accounts => Set<BankAccount>();
        public DbSet<Card> Cards => Set<Card>();
        public DbSet<BankTransaction> Transactions => Set<BankTransaction>();
        public DbSet<Loan> Loans => Set<Loan>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Branch>().Property(x => x.Name).HasMaxLength(100).IsRequired();
            builder.Entity<Branch>().Property(x => x.Address).HasMaxLength(200).IsRequired();

            builder.Entity<CustomerProfile>().HasIndex(x => x.IdentityUserId).IsUnique();

            builder.Entity<BankAccount>().HasIndex(x => x.AccountNumber).IsUnique();
            builder.Entity<BankAccount>().Property(x => x.Balance).HasPrecision(18, 2);
            builder.Entity<BankAccount>()
                .HasOne(x => x.CustomerProfile)
                .WithMany(x => x.Accounts)
                .HasForeignKey(x => x.CustomerProfileId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Card>().HasIndex(x => x.CardNumber).IsUnique();
            builder.Entity<Card>()
                .HasOne(x => x.Account)
                .WithOne(x => x.Card)
                .HasForeignKey<Card>(x => x.AccountId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<BankTransaction>().Property(x => x.Amount).HasPrecision(18, 2);
            builder.Entity<BankTransaction>()
                .HasOne(x => x.Account)
                .WithMany(x => x.Transactions)
                .HasForeignKey(x => x.AccountId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Loan>().Property(x => x.Amount).HasPrecision(18, 2);
            builder.Entity<Loan>().Property(x => x.InterestRate).HasPrecision(5, 2);
            builder.Entity<Loan>()
                .HasOne(x => x.CustomerProfile)
                .WithMany(x => x.Loans)
                .HasForeignKey(x => x.CustomerProfileId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

            services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequiredLength = 8;
            })
                .AddEntityFrameworkStores<AppDbContext>()
                .AddDefaultTokenProviders();

            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IAdminService, AdminService>();
            services.AddScoped<ITellerService, TellerService>();
            services.AddScoped<ICustomerService, CustomerService>();

            return services;
        }
    }






    public static class DbSeeder
    {
        public static async Task SeedAsync(IServiceProvider services, IConfiguration configuration)
        {
            using var scope = services.CreateScope();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            foreach (var role in new[] { RoleNames.Admin, RoleNames.Teller, RoleNames.Customer })
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            var adminEmail = configuration["Seed:AdminEmail"] ?? "admin@bank.local";
            var adminPassword = configuration["Seed:AdminPassword"] ?? "Admin@12345";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser is null)
            {
                adminUser = new ApplicationUser { UserName = adminEmail, Email = adminEmail, EmailConfirmed = true };
                var result = await userManager.CreateAsync(adminUser, adminPassword);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, RoleNames.Admin);
                }
            }
        }
    }
}

