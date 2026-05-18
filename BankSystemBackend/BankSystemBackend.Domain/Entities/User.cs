using BankSystemBackend.Enums;
using Microsoft.AspNetCore.Identity;
using System;

namespace BankSystemBackend.Models
{
    public class AppUser : IdentityUser<int>
    {
        public string? PhotoUrl { get; set; }

        public UserRole Role { get; set; }

        public string Status { get; set; } = "Active"; // "Active", "Suspended"

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }
}