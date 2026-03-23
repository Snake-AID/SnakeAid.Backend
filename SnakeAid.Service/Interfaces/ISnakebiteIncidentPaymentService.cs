using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SnakeAid.Core.Requests.PayOs;
using SnakeAid.Core.Requests.PayOS;
using SnakeAid.Core.Responses.PayOs;
using SnakeAid.Core.Responses.PayOS;

namespace SnakeAid.Service.Interfaces
{
    public interface ISnakebiteIncidentPaymentService
    {
        Task<SnakebiteIncidentPaymentResponse> CreateSnakebiteIncidentPaymentLinkAsync(
            CreateSnakebiteIncidentPaymentRequest request,
            Guid currentUserId,
            CancellationToken cancellationToken);

        Task<SnakebiteIncidentPaymentResponse> CreateSnakebiteIncidentWalletPaymentAsync(
            CreateSnakebiteIncidentPaymentRequest request,
            Guid currentUserId,
            CancellationToken cancellationToken);

        Task<CancelPaymentLinkResponse> CancelSnakebiteIncidentPaymentLinkAsync(
            long orderCode,
            CancelPaymentLinkRequest request,
            CancellationToken cancellationToken);

        Task<PayOsWebhookResponse> ProcessSnakebiteIncidentWebhookAsync(
            string rawPayload,
            CancellationToken cancellationToken);

        Task<PayOsWebhookResponse> ConfirmSnakebiteIncidentPaymentAsync(
            Guid transactionId,
            CancellationToken cancellationToken);

        Task<PayOsWebhookResponse> ConfirmSnakebiteIncidentPaymentByOrderCodeAsync(
            long orderCode,
            CancellationToken cancellationToken);

        Task<RefundTransactionResponse> RefundSnakebiteIncidentTransactionAsync(
            RefundTransactionRequest request,
            CancellationToken cancellationToken);
    }
}