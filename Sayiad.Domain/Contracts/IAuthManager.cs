using Sayiad.Domain.Dtos.AuthDtos;

namespace Sayiad.Domain.Contracts;

public interface IAuthManager
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request);
    Task<AuthResponse> LoginAsync(LoginRequest request);
    Task<AuthResponse> RefreshTokenAsync(string refreshToken);
    Task ChangePasswordAsync(int userId, string currentPassword, string newPassword);
}
