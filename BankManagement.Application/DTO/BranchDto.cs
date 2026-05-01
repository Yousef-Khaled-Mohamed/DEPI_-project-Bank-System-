using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace BankManagement.Application.DTO
{
    public class BranchDto
    {
        public int Id { get; set; }
        [Required, StringLength(100)]
        public string Name { get; set; } = string.Empty;
        [Required, StringLength(200)]
        public string Address { get; set; } = string.Empty;
    }

}
