using Sayiad.Domain.Contracts;
using Sayiad.Data.Data;

namespace Sayiad.Data.Repositories;

public class AuctionRepository : IAuctionRepository
{
    private readonly ApplicationDbContext _db;

    public AuctionRepository(ApplicationDbContext db) => _db = db;

    public async Task<IEnumerable<Auction>> GetActiveAsync()
    {
        return await _db.Auctions
            .Include(a => a.Product).ThenInclude(p => p.Images)
            .Include(a => a.Bids)
            .Where(a => a.Status == AuctionStatus.Active)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();
    }

    public async Task<Auction?> GetByIdAsync(int auctionId)
    {
        return await _db.Auctions.FindAsync(auctionId);
    }

    public async Task<Auction?> GetByIdWithBidsAsync(int auctionId)
    {
        return await _db.Auctions
            .Include(a => a.Bids)
            .FirstOrDefaultAsync(a => a.Id == auctionId);
    }

    public async Task<Auction?> GetByIdWithDetailsAsync(int auctionId)
    {
        return await _db.Auctions
            .Include(a => a.Product).ThenInclude(p => p.Images)
            .Include(a => a.Bids).ThenInclude(b => b.User)
            .Include(a => a.Winner)
            .FirstOrDefaultAsync(a => a.Id == auctionId);
    }

    public async Task AddAsync(Auction auction)
    {
        _db.Auctions.Add(auction);
        await _db.SaveChangesAsync();
    }

    public async Task<int> SaveChangesAsync()
    {
        return await _db.SaveChangesAsync();
    }
}
