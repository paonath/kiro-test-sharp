namespace BoltWebAPI.Models.Entities;

public class ConfigurationChangeEntity
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public DateTime ChangedAt { get; set; }
    public string PreviousConfiguration { get; set; } = string.Empty;
    public string NewConfiguration { get; set; } = string.Empty;
    public string ConfigurationType { get; set; } = string.Empty;
    public string ChangeDescription { get; set; } = string.Empty;
}
