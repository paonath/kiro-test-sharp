namespace BoltWebAPI.Models.Entities;

public class RefreshTokenEntity
{
    public string Token { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsRevoked { get; set; }
    
    // Navigation property
    public UserEntity User { get; set; } = null!;
}
