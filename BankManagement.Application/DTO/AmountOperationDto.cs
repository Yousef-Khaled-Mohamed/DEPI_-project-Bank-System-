using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace BankManagement.Application.DTO
{
    public class AmountOperationDto
    {
        [Required]
        public int AccountId { get; set; }

        [Range(0.01, 999999999)]
        public decimal Amount { get; set; }
    }
}
