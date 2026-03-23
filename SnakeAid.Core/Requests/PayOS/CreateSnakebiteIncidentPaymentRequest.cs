using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SnakeAid.Core.Domains;

namespace SnakeAid.Core.Requests.PayOS
{
    public class CreateSnakebiteIncidentPaymentRequest
    {
        public Guid SnakebiteIncidentId { get; set; }
        public decimal Amount { get; set; }
        public string? Description { get; set; }
        public TransactionType TransactionType { get; set; } = TransactionType.SnakebiteIncidentPayment;
    }
}