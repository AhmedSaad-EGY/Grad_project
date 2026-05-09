using Microsoft.Extensions.Logging;
using Sayiad.Domain.Contracts;
using Sayiad.Domain.Enums;
using Sayiad.Domain.Models;
using Sayiad.Domain.Dtos.AuthDtos;

namespace Sayiad.Domain.Managers;

public class AuthManager : IAuthManager
{
    private readonly IUserRepository _userRepo;
    private readonly ITokenService _tokenService;
    private readonly ILogger<AuthManager> _logger;

    public AuthManager(
        IUserRepository userRepo,
        ITokenService tokenService,
        ILogger<AuthManager> logger)
    {
        _userRepo = userRepo;
        _tokenService = tokenService;
        _logger = logger;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        if (await _userRepo.EmailExistsAsync(request.Email))
            throw new InvalidOperationException("Email already registered");

        var user = new User
        {
            FullName = request.FullName,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Phone = request.Phone,
            Role = Enum.Parse<UserRole>(request.Role),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _userRepo.AddAsync(user);
        _logger.LogInformation("User registered: {Email} as {Role}", user.Email, user.Role);
        return await GenerateAuthResponse(user);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var user = await _userRepo.GetByEmailAsync(request.Email)
            ?? throw new UnauthorizedAccessException("Invalid email or password");

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid email or password");

        if (!user.IsActive)
            throw new UnauthorizedAccessException("Account is disabled");

        _logger.LogInformation("User logged in: {Email}", user.Email);
        return await GenerateAuthResponse(user);
    }

    public async Task<AuthResponse> RefreshTokenAsync(string refreshToken)
    {
        var user = await _userRepo.GetByRefreshTokenAsync(refreshToken)
            ?? throw new UnauthorizedAccessException("Invalid or expired refresh token");

        _logger.LogInformation("Token refreshed for user: {Email}", user.Email);
        return await GenerateAuthResponse(user);
    }

    public async Task ChangePasswordAsync(int userId, string currentPassword, string newPassword)
    {
        var user = await _userRepo.GetByIdAsync(userId)
            ?? throw new KeyNotFoundException("User not found");

        if (!BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash))
            throw new UnauthorizedAccessException("Current password is incorrect");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        user.UpdatedAt = DateTime.UtcNow;
        await _userRepo.UpdateAsync(user);

        _logger.LogInformation("Password changed for user: {UserId}", userId);
    }

    private async Task<AuthResponse> GenerateAuthResponse(User user)
    {
        var (token, expiry) = _tokenService.GenerateJwtToken(user);
        var refreshToken = _tokenService.GenerateRefreshToken();

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
        await _userRepo.UpdateAsync(user);

        return new AuthResponse(
            Token: token,
            RefreshToken: refreshToken,
            ExpiresAt: expiry,
            User: MapUser(user)
        );
    }

    private static UserInfo MapUser(User user) => new(
        user.Id,
        user.FullName,
        user.Email,
        user.Phone,
        user.ProfileImage,
        user.Role.ToString(),
        user.IsActive
    );
}