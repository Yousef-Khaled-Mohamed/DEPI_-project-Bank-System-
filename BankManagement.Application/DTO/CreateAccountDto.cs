using BankManagement.Domain.Enum;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace BankManagement.Application.DTO
{
    public class CreateAccountDto
    {
        [Required]
        public int CustomerProfileId { get; set; }

        [Required]
        public AccountType AccountType { get; set; }
    }
}
