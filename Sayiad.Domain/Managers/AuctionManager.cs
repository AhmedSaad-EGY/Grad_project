using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sayiad.Domain.Contracts;
using Sayiad.Domain.Enums;
using Sayiad.Domain.Models;
using Sayiad.Domain.Dtos.AuctionDtos;

namespace Sayiad.Domain.Managers;

public class AuctionManager : IAuctionManager
{
    private readonly IAuctionRepository _auctionRepo;
    private readonly IProductRepository _productRepo;
    private readonly ILogger<AuctionManager> _logger;

    public AuctionManager(
        IAuctionRepository auctionRepo,
        IProductRepository productRepo,
        ILogger<AuctionManager> logger)
    {
        _auctionRepo = auctionRepo;
        _productRepo = productRepo;
        _logger = logger;
    }

    public async Task<IEnumerable<AuctionResponse>> GetActiveAsync()
    {
        var auctions = await _auctionRepo.GetActiveAsync();
        return auctions.Select(MapToResponse);
    }

    public async Task<AuctionDetailResponse> GetByIdAsync(int auctionId)
    {
        var auction = await _auctionRepo.GetByIdWithDetailsAsync(auctionId)
            ?? throw new KeyNotFoundException("Auction not found");

        return new AuctionDetailResponse(
            MapToResponse(auction),
            auction.Bids.OrderByDescending(b => b.Amount)
                .Select(b => new BidResponse(
                    b.Id, b.UserId, b.User!.FullName, b.Amount,
                    b.IsAutoBid, b.BidStatus.ToString(), b.CreatedAt))
                .ToList()
        );
    }

    public async Task<AuctionResponse> CreateAsync(int userId, CreateAuctionRequest request)
    {
        var product = await _productRepo.GetByIdAsync(request.ProductId)
            ?? throw new KeyNotFoundException("Product not found");

        if (product.SellerId != userId)
            throw new UnauthorizedAccessException("You can only auction your own products");

        var auction = new Auction
        {
            ProductId = request.ProductId,
            StartTime = DateTime.UtcNow,
            EndTime = request.EndTime,
            StartingPrice = request.StartingPrice,
            ReservePrice = request.ReservePrice,
            MinimumIncrement = request.MinimumIncrement,
            CurrentHighestBid = request.StartingPrice,
            Status = AuctionStatus.Active,
            CreatedAt = DateTime.UtcNow
        };

        product.IsAuctioned = true;
        await _auctionRepo.AddAsync(auction);

        var saved = await _auctionRepo.GetByIdWithDetailsAsync(auction.Id)
            ?? throw new InvalidOperationException("Failed to load saved auction");

        _logger.LogInformation("Auction created: {AuctionId} for product {ProductId}", auction.Id, request.ProductId);
        return MapToResponse(saved);
    }

    public async Task<BidResponse> PlaceBidAsync(int auctionId, int userId, PlaceBidRequest request)
    {
        const int maxRetries = 3;
        var amount = request.Amount;

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                return await PlaceBidInternalAsync(auctionId, userId, amount);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogWarning(ex,
                    "Concurrency conflict placing bid on auction {AuctionId}, attempt {Attempt}/{MaxRetries}",
                    auctionId, attempt, maxRetries);

                if (attempt == maxRetries)
                    throw new InvalidOperationException("Bid failed due to concurrent activity. Please try again.");
            }
        }

        throw new InvalidOperationException("Bid placement failed");
    }

    private async Task<BidResponse> PlaceBidInternalAsync(int auctionId, int userId, decimal amount)
    {
        var auction = await _auctionRepo.GetByIdWithBidsAsync(auctionId)
            ?? throw new KeyNotFoundException("Auction not found");

        if (auction.Status != AuctionStatus.Active)
            throw new InvalidOperationException("Auction is not active");

        if (amount < auction.CurrentHighestBid + auction.MinimumIncrement)
            throw new InvalidOperationException(
                $"Bid must be at least {auction.CurrentHighestBid + auction.MinimumIncrement:C}");

        if (auction.EndTime <= DateTime.UtcNow)
        {
            auction.Status = AuctionStatus.Finished;
            await _auctionRepo.SaveChangesAsync();
            throw new InvalidOperationException("Auction has ended");
        }

        foreach (var prevBid in auction.Bids.Where(b => b.BidStatus == BidStatus.Winning))
        {
            prevBid.BidStatus = BidStatus.Valid;
        }

        var bid = new Bid
        {
            AuctionId = auctionId,
            UserId = userId,
            Amount = amount,
            IsAutoBid = false,
            BidStatus = BidStatus.Winning,
            CreatedAt = DateTime.UtcNow
        };

        auction.Bids.Add(bid);
        auction.CurrentHighestBid = amount;

        await _auctionRepo.SaveChangesAsync();

        _logger.LogInformation("Bid placed: {BidAmount} on auction {AuctionId} by user {UserId}",
            amount, auctionId, userId);

        return new BidResponse(
            bid.Id, bid.UserId, string.Empty, bid.Amount,
            bid.IsAutoBid, bid.BidStatus.ToString(), bid.CreatedAt);
    }

    public async Task<AuctionResponse> EndAuctionAsync(int auctionId)
    {
        var auction = await _auctionRepo.GetByIdWithDetailsAsync(auctionId)
            ?? throw new KeyNotFoundException("Auction not found");

        if (auction.Status != AuctionStatus.Active)
            throw new InvalidOperationException("Auction is already finished or cancelled");

        auction.Status = AuctionStatus.Finished;

        var winningBid = auction.Bids
            .Where(b => b.BidStatus == BidStatus.Winning)
            .MaxBy(b => b.Amount);

        if (winningBid != null && winningBid.Amount >= auction.ReservePrice)
        {
            auction.WinnerUserId = winningBid.UserId;

            if (auction.Product != null)
            {
                auction.Product.Status = ProductStatus.Sold;
            }
        }

        await _auctionRepo.SaveChangesAsync();

        _logger.LogInformation("Auction ended: {AuctionId}, winner: {WinnerId}",
            auctionId, auction.WinnerUserId);

        return MapToResponse(auction);
    }

    private static AuctionResponse MapToResponse(Auction auction) => new(
        auction.Id, auction.ProductId, auction.Product!.Title,
        auction.Product.Images.FirstOrDefault(i => i.IsPrimary)?.ImageUrl,
        auction.WinnerUserId, auction.Winner?.FullName,
        auction.StartTime, auction.EndTime,
        auction.StartingPrice, auction.ReservePrice,
        auction.MinimumIncrement, auction.CurrentHighestBid,
        auction.Status, auction.Bids.Count, auction.CreatedAt);
}