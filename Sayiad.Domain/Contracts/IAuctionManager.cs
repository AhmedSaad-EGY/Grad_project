using Sayiad.Domain.Dtos.AuctionDtos;

namespace Sayiad.Domain.Contracts;

public interface IAuctionManager
{
    Task<IEnumerable<AuctionResponse>> GetActiveAsync();
    Task<AuctionDetailResponse> GetByIdAsync(int auctionId);
    Task<AuctionResponse> CreateAsync(int userId, CreateAuctionRequest request);
    Task<BidResponse> PlaceBidAsync(int auctionId, int userId, PlaceBidRequest request);
    Task<AuctionResponse> EndAuctionAsync(int auctionId);
}
