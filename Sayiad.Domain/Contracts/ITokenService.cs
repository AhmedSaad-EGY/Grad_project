using Sayiad.Domain.Models;

namespace Sayiad.Domain.Contracts;

public interface ITokenService
{
    (string Token, DateTime Expiry) GenerateJwtToken(User user);
    string GenerateRefreshToken();
}
