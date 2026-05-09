namespace Sayiad.Domain.Contracts;

public interface IAuctionRepository
{
    Task<IEnumerable<Auction>> GetActiveAsync();
    Task<Auction?> GetByIdAsync(int auctionId);
    Task<Auction?> GetByIdWithBidsAsync(int auctionId);
    Task<Auction?> GetByIdWithDetailsAsync(int auctionId);
    Task AddAsync(Auction auction);
    Task<int> SaveChangesAsync();
}
