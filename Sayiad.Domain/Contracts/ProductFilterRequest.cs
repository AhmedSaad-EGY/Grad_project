using Sayiad.Domain.Enums;

namespace Sayiad.Domain.Contracts;

public record ProductFilterRequest(
    int? CategoryId,
    decimal? MinPrice,
    decimal? MaxPrice,
    ProductCondition? Condition,
    string? Location,
    string? SearchTerm
);
