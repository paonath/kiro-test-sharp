using BoltWebAPI.Models.Domain;

namespace BoltWebAPI.Services.Interfaces;

public interface IAuthenticationService
{
    Task<AuthenticationResponse> AuthenticateAsync(string username, string password);
    Task<AuthenticationResponse> RefreshTokenAsync(string refreshToken);
    Task<bool> ValidateTokenAsync(string token);
    Task RevokeTokenAsync(string token);
}
