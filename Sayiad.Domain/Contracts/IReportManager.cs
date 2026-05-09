using Sayiad.Domain.Dtos.ReportDtos;

namespace Sayiad.Domain.Contracts;

public interface IReportManager
{
    Task<ReportResponse> CreateAsync(int reporterId, CreateReportRequest request);
    Task<IEnumerable<ReportResponse>> GetAllAsync();
    Task<ReportResponse> GetByIdAsync(int reportId);
    Task<ReportResponse> ResolveAsync(int reportId, ResolveReportRequest request);
}
