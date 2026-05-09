using Sayiad.Domain.Dtos.PaymentDtos;

namespace Sayiad.Domain.Contracts;

public interface IPaymentManager
{
    Task<PaymentResponse> InitiateAsync(int userId, InitiatePaymentRequest request);
    Task<PaymentResponse> ConfirmAsync(int paymentId);
    Task<IEnumerable<PaymentResponse>> GetOrderPaymentsAsync(int orderId, int userId);
}
