using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace BankManagement.Application.DTO
{
    public class CreateLoanDto
    {
        [Required]
        public int CustomerProfileId { get; set; }

        [Range(0.01, 999999999)]
        public decimal Amount { get; set; }

        [Range(0.1, 100)]
        public decimal InterestRate { get; set; }

        [Range(1, 480)]
        public int DurationMonths { get; set; }
    }
}
