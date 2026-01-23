using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace SnakeAid.Core.Domains
{
    public class PaymentCard : BaseEntity
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public string CardNumber { get; set; }

        [Required]
        public string CardHolderName { get; set; }

        [Required]
        public DateTime ExpiryDate { get; set; }

        [Required]
        public string Cvv { get; set; }

        public bool IsDefault { get; set; } = false;
    }
}