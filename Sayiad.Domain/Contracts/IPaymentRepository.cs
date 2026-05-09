namespace Sayiad.Domain.Contracts;

public interface IPaymentRepository
{
    Task<Payment?> GetByIdAsync(int id);
    Task<IEnumerable<Payment>> GetOrderPaymentsAsync(int orderId);
    Task AddAsync(Payment payment);
    Task UpdateAsync(Payment payment);
}
