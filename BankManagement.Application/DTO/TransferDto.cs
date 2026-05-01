using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace BankManagement.Application.DTO
{
    public class TransferDto
    {
        [Required]
        public int SourceAccountId { get; set; }

        [Required]
        public int TargetAccountId { get; set; }

        [Range(0.01, 999999999)]
        public decimal Amount { get; set; }
    }
}
