using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using SnakeAid.Core.Domains;

namespace SnakeAid.Core.Requests.PayOS
{
    public class CreateSnakebiteIncidentPaymentRequest
    {
        [Required(ErrorMessage = "SnakebiteIncidentId is required.")]
        public Guid SnakebiteIncidentId { get; set; }
        [Required(ErrorMessage = "Amount is required.")]
        public decimal Amount { get; set; }
        public string? Description { get; set; }
        public TransactionType TransactionType { get; set; } = TransactionType.SnakebiteIncidentPayment;
    }
}