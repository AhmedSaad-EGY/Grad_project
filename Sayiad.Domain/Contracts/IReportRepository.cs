namespace Sayiad.Domain.Contracts;

public interface IReportRepository
{
    Task<IEnumerable<Report>> GetAllAsync();
    Task<Report?> GetByIdAsync(int reportId);
    Task<bool> ExistsByReporterAndProductAsync(int reporterId, int productId);
    Task AddAsync(Report report);
    Task UpdateAsync(Report report);
}
