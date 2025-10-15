using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using BoltWebAPI.Data;
using BoltWebAPI.Models.Domain;
using BoltWebAPI.Models.Entities;
using BoltWebAPI.Services.Interfaces;

namespace BoltWebAPI.Services.Implementations;

public class AuthenticationService : IAuthenticationService
{
    private readonly BoltDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthenticationService> _logger;

    public AuthenticationService(
        BoltDbContext context,
        IConfiguration configuration,
        ILogger<AuthenticationService> logger)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<AuthenticationResponse> AuthenticateAsync(string username, string password)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Username == username && u.IsActive);

        if (user == null)
        {
            _logger.LogWarning("Authentication failed for username: {Username}", username);
            throw new UnauthorizedAccessException("Invalid username or password");
        }

        if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
        {
            _logger.LogWarning("Invalid password for username: {Username}", username);
            throw new UnauthorizedAccessException("Invalid username or password");
        }

        // Update last login
        user.LastLoginAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        var accessToken = GenerateAccessToken(user);
        var refreshToken = await GenerateRefreshTokenAsync(user.Id);

        _logger.LogInformation("User {Username} authenticated successfully", username);

        return new AuthenticationResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(GetJwtExpirationMinutes()),
            Username = user.Username,
            Role = user.Role
        };
    }

    public async Task<AuthenticationResponse> RefreshTokenAsync(string refreshToken)
    {
        var tokenEntity = await _context.RefreshTokens
            .FirstOrDefaultAsync(t => t.Token == refreshToken && !t.IsRevoked);

        if (tokenEntity == null || tokenEntity.ExpiresAt < DateTime.UtcNow)
        {
            _logger.LogWarning("Invalid or expired refresh token");
            throw new UnauthorizedAccessException("Invalid or expired refresh token");
        }

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == tokenEntity.UserId && u.IsActive);

        if (user == null)
        {
            _logger.LogWarning("User not found for refresh token");
            throw new UnauthorizedAccessException("User not found");
        }

        // Revoke old refresh token
        tokenEntity.IsRevoked = true;

        var accessToken = GenerateAccessToken(user);
        var newRefreshToken = await GenerateRefreshTokenAsync(user.Id);

        await _context.SaveChangesAsync();

        _logger.LogInformation("Token refreshed for user {Username}", user.Username);

        return new AuthenticationResponse
        {
            AccessToken = accessToken,
            RefreshToken = newRefreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(GetJwtExpirationMinutes()),
            Username = user.Username,
            Role = user.Role
        };
    }

    public Task<bool> ValidateTokenAsync(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(GetJwtSecret());

            tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = GetJwtIssuer(),
                ValidateAudience = true,
                ValidAudience = GetJwtAudience(),
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            }, out SecurityToken validatedToken);

            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Token validation failed");
            return Task.FromResult(false);
        }
    }

    public async Task RevokeTokenAsync(string token)
    {
        var tokenEntity = await _context.RefreshTokens
            .FirstOrDefaultAsync(t => t.Token == token);

        if (tokenEntity != null)
        {
            tokenEntity.IsRevoked = true;
            await _context.SaveChangesAsync();
            _logger.LogInformation("Refresh token revoked");
        }
    }

    private string GenerateAccessToken(UserEntity user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(GetJwtSecret());

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(GetJwtExpirationMinutes()),
            Issuer = GetJwtIssuer(),
            Audience = GetJwtAudience(),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    private async Task<string> GenerateRefreshTokenAsync(string userId)
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        var refreshToken = Convert.ToBase64String(randomBytes);

        var tokenEntity = new RefreshTokenEntity
        {
            Token = refreshToken,
            UserId = userId,
            ExpiresAt = DateTime.UtcNow.AddDays(GetRefreshTokenExpirationDays()),
            CreatedAt = DateTime.UtcNow,
            IsRevoked = false
        };

        _context.RefreshTokens.Add(tokenEntity);
        await _context.SaveChangesAsync();

        return refreshToken;
    }

    private string GetJwtSecret()
    {
        return _configuration["Authentication:JwtSecret"]
            ?? throw new InvalidOperationException("JWT secret not configured");
    }

    private int GetJwtExpirationMinutes()
    {
        return _configuration.GetValue<int>("Authentication:JwtExpirationMinutes", 15);
    }

    private int GetRefreshTokenExpirationDays()
    {
        return _configuration.GetValue<int>("Authentication:RefreshTokenExpirationDays", 7);
    }

    private string GetJwtIssuer()
    {
        return _configuration["Authentication:Issuer"] ?? "BoltWebAPI";
    }

    private string GetJwtAudience()
    {
        return _configuration["Authentication:Audience"] ?? "BoltWebClient";
    }
}
