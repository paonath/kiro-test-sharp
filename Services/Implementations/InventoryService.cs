using BoltWebAPI.Data;
using BoltWebAPI.Models.Domain;
using BoltWebAPI.Models.Entities;
using BoltWebAPI.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace BoltWebAPI.Services.Implementations;

/// <summary>
/// Service for managing Bolt inventory with YAML parsing
/// </summary>
public class InventoryService : IInventoryService
{
    private readonly IConfiguration _configuration;
    private readonly BoltDbContext _dbContext;
    private readonly ILogger<InventoryService> _logger;
    private readonly IDeserializer _yamlDeserializer;
    private readonly ISerializer _yamlSerializer;

    public InventoryService(
        IConfiguration configuration,
        BoltDbContext dbContext,
        ILogger<InventoryService> logger)
    {
        _configuration = configuration;
        _dbContext = dbContext;
        _logger = logger;

        _yamlDeserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        _yamlSerializer = new SerializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .Build();
    }

    private string GetInventoryFilePath()
    {
        var inventoryPath = _configuration["Bolt:InventoryFile"] ?? "inventory.yaml";
        return Path.GetFullPath(inventoryPath);
    }

    public async Task<Inventory> GetInventoryAsync()
    {
        var inventoryPath = GetInventoryFilePath();

        if (!File.Exists(inventoryPath))
        {
            _logger.LogWarning("Inventory file not found at {Path}, returning empty inventory", inventoryPath);
            return new Inventory();
        }

        try
        {
            var yamlContent = await File.ReadAllTextAsync(inventoryPath);
            var inventory = _yamlDeserializer.Deserialize<Inventory>(yamlContent);

            _logger.LogDebug("Loaded inventory with {NodeCount} nodes and {GroupCount} groups",
                inventory.Nodes.Count, inventory.Groups.Count);

            return inventory ?? new Inventory();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse inventory file at {Path}", inventoryPath);
            throw new InvalidOperationException($"Failed to parse inventory file: {ex.Message}", ex);
        }
    }

    public async Task<IEnumerable<InventoryNode>> GetNodesAsync()
    {
        var inventory = await GetInventoryAsync();
        return inventory.Nodes;
    }

    public async Task<InventoryNode> GetNodeAsync(string nodeUri)
    {
        var inventory = await GetInventoryAsync();
        var node = inventory.Nodes.FirstOrDefault(n => n.Uri.Equals(nodeUri, StringComparison.OrdinalIgnoreCase));

        if (node == null)
        {
            throw new KeyNotFoundException($"Node '{nodeUri}' not found in inventory");
        }

        return node;
    }

    public async Task<IEnumerable<InventoryGroup>> GetGroupsAsync()
    {
        var inventory = await GetInventoryAsync();
        return inventory.Groups;
    }

    public async Task<InventoryGroup> GetGroupAsync(string groupName)
    {
        var inventory = await GetInventoryAsync();
        var group = inventory.Groups.FirstOrDefault(g => g.Name.Equals(groupName, StringComparison.OrdinalIgnoreCase));

        if (group == null)
        {
            throw new KeyNotFoundException($"Group '{groupName}' not found in inventory");
        }

        return group;
    }

    public async Task<IEnumerable<InventoryNode>> GetGroupNodesAsync(string groupName)
    {
        var inventory = await GetInventoryAsync();
        var group = inventory.Groups.FirstOrDefault(g => g.Name.Equals(groupName, StringComparison.OrdinalIgnoreCase));

        if (group == null)
        {
            throw new KeyNotFoundException($"Group '{groupName}' not found in inventory");
        }

        // Get nodes that are directly in the group
        var nodes = inventory.Nodes.Where(n => group.Nodes.Contains(n.Uri, StringComparer.OrdinalIgnoreCase)).ToList();

        return nodes;
    }

    public async Task<InventoryNode> AddNodeAsync(AddNodeRequest request, string userId)
    {
        var inventory = await GetInventoryAsync();

        // Check if node already exists
        if (inventory.Nodes.Any(n => n.Uri.Equals(request.Uri, StringComparison.OrdinalIgnoreCase)))
        {
            throw new ArgumentException($"Node with URI '{request.Uri}' already exists");
        }

        var newNode = new InventoryNode
        {
            Uri = request.Uri,
            Name = request.Name,
            Groups = request.Groups,
            Config = request.Config
        };

        inventory.Nodes.Add(newNode);

        // Add node to specified groups
        foreach (var groupName in request.Groups)
        {
            var group = inventory.Groups.FirstOrDefault(g => g.Name.Equals(groupName, StringComparison.OrdinalIgnoreCase));
            if (group != null && !group.Nodes.Contains(request.Uri, StringComparer.OrdinalIgnoreCase))
            {
                group.Nodes.Add(request.Uri);
            }
        }

        await SaveInventoryAsync(inventory, userId, $"Added node: {request.Uri}");

        _logger.LogInformation("Added node {NodeUri} to inventory (User: {UserId})", request.Uri, userId);

        return newNode;
    }

    public async Task<InventoryNode> UpdateNodeAsync(string nodeUri, AddNodeRequest request, string userId)
    {
        var inventory = await GetInventoryAsync();
        var node = inventory.Nodes.FirstOrDefault(n => n.Uri.Equals(nodeUri, StringComparison.OrdinalIgnoreCase));

        if (node == null)
        {
            throw new KeyNotFoundException($"Node '{nodeUri}' not found in inventory");
        }

        // Update node properties
        node.Uri = request.Uri;
        node.Name = request.Name;
        node.Config = request.Config;

        // Update group memberships
        var oldGroups = node.Groups.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var newGroups = request.Groups.ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Remove from old groups
        foreach (var groupName in oldGroups.Except(newGroups))
        {
            var group = inventory.Groups.FirstOrDefault(g => g.Name.Equals(groupName, StringComparison.OrdinalIgnoreCase));
            if (group != null)
            {
                group.Nodes.RemoveAll(n => n.Equals(nodeUri, StringComparison.OrdinalIgnoreCase));
            }
        }

        // Add to new groups
        foreach (var groupName in newGroups.Except(oldGroups))
        {
            var group = inventory.Groups.FirstOrDefault(g => g.Name.Equals(groupName, StringComparison.OrdinalIgnoreCase));
            if (group != null && !group.Nodes.Contains(request.Uri, StringComparer.OrdinalIgnoreCase))
            {
                group.Nodes.Add(request.Uri);
            }
        }

        node.Groups = request.Groups;

        await SaveInventoryAsync(inventory, userId, $"Updated node: {nodeUri}");

        _logger.LogInformation("Updated node {NodeUri} in inventory (User: {UserId})", nodeUri, userId);

        return node;
    }

    public async Task DeleteNodeAsync(string nodeUri, string userId)
    {
        var inventory = await GetInventoryAsync();
        var node = inventory.Nodes.FirstOrDefault(n => n.Uri.Equals(nodeUri, StringComparison.OrdinalIgnoreCase));

        if (node == null)
        {
            throw new KeyNotFoundException($"Node '{nodeUri}' not found in inventory");
        }

        inventory.Nodes.Remove(node);

        // Remove node from all groups
        foreach (var group in inventory.Groups)
        {
            group.Nodes.RemoveAll(n => n.Equals(nodeUri, StringComparison.OrdinalIgnoreCase));
        }

        await SaveInventoryAsync(inventory, userId, $"Deleted node: {nodeUri}");

        _logger.LogInformation("Deleted node {NodeUri} from inventory (User: {UserId})", nodeUri, userId);
    }

    public async Task<InventoryGroup> UpsertGroupAsync(UpsertGroupRequest request, string userId)
    {
        var inventory = await GetInventoryAsync();
        var group = inventory.Groups.FirstOrDefault(g => g.Name.Equals(request.Name, StringComparison.OrdinalIgnoreCase));

        if (group == null)
        {
            // Create new group
            group = new InventoryGroup
            {
                Name = request.Name,
                Nodes = request.Nodes,
                Groups = request.Groups,
                Config = request.Config
            };

            inventory.Groups.Add(group);

            _logger.LogInformation("Created group {GroupName} in inventory (User: {UserId})", request.Name, userId);
        }
        else
        {
            // Update existing group
            group.Nodes = request.Nodes;
            group.Groups = request.Groups;
            group.Config = request.Config;

            _logger.LogInformation("Updated group {GroupName} in inventory (User: {UserId})", request.Name, userId);
        }

        await SaveInventoryAsync(inventory, userId, $"Upserted group: {request.Name}");

        return group;
    }

    public async Task DeleteGroupAsync(string groupName, string userId)
    {
        var inventory = await GetInventoryAsync();
        var group = inventory.Groups.FirstOrDefault(g => g.Name.Equals(groupName, StringComparison.OrdinalIgnoreCase));

        if (group == null)
        {
            throw new KeyNotFoundException($"Group '{groupName}' not found in inventory");
        }

        inventory.Groups.Remove(group);

        await SaveInventoryAsync(inventory, userId, $"Deleted group: {groupName}");

        _logger.LogInformation("Deleted group {GroupName} from inventory (User: {UserId})", groupName, userId);
    }

    public async Task<Inventory> UpdateInventoryAsync(UpdateInventoryRequest request, string userId)
    {
        // Validate YAML first
        var validationResult = await ValidateInventoryAsync(request.YamlContent);
        if (!validationResult.IsValid)
        {
            throw new ArgumentException($"Invalid inventory YAML: {string.Join(", ", validationResult.Errors)}");
        }

        if (request.ValidateOnly)
        {
            // Just return the parsed inventory without saving
            var validatedInventory = _yamlDeserializer.Deserialize<Inventory>(request.YamlContent);
            return validatedInventory ?? new Inventory();
        }

        var inventory = _yamlDeserializer.Deserialize<Inventory>(request.YamlContent);
        if (inventory == null)
        {
            throw new ArgumentException("Failed to parse inventory YAML");
        }

        var inventoryPath = GetInventoryFilePath();
        await File.WriteAllTextAsync(inventoryPath, request.YamlContent);

        await LogConfigurationChangeAsync(userId, "inventory", "Updated entire inventory from YAML");

        _logger.LogInformation("Updated inventory from YAML (User: {UserId})", userId);

        return inventory;
    }

    public async Task<ConfigurationValidationResult> ValidateInventoryAsync(string yamlContent)
    {
        var result = new ConfigurationValidationResult { IsValid = true };

        try
        {
            var inventory = _yamlDeserializer.Deserialize<Inventory>(yamlContent);

            if (inventory == null)
            {
                result.IsValid = false;
                result.Errors.Add("Failed to parse inventory YAML");
                return result;
            }

            // Validate structure
            if (inventory.Version != 2)
            {
                result.Warnings.Add($"Inventory version is {inventory.Version}, expected 2");
            }

            // Validate node URIs are unique
            var duplicateUris = inventory.Nodes
                .GroupBy(n => n.Uri, StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (duplicateUris.Any())
            {
                result.IsValid = false;
                result.Errors.Add($"Duplicate node URIs found: {string.Join(", ", duplicateUris)}");
            }

            // Validate group names are unique
            var duplicateGroups = inventory.Groups
                .GroupBy(g => g.Name, StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (duplicateGroups.Any())
            {
                result.IsValid = false;
                result.Errors.Add($"Duplicate group names found: {string.Join(", ", duplicateGroups)}");
            }
        }
        catch (Exception ex)
        {
            result.IsValid = false;
            result.Errors.Add($"YAML parsing error: {ex.Message}");
        }

        return result;
    }

    private async Task SaveInventoryAsync(Inventory inventory, string userId, string changeDescription)
    {
        var inventoryPath = GetInventoryFilePath();
        var yamlContent = _yamlSerializer.Serialize(inventory);

        await File.WriteAllTextAsync(inventoryPath, yamlContent);

        await LogConfigurationChangeAsync(userId, "inventory", changeDescription);
    }

    private async Task LogConfigurationChangeAsync(string userId, string configType, string changeDescription)
    {
        var change = new ConfigurationChangeEntity
        {
            UserId = userId,
            ConfigurationType = configType,
            ChangeDescription = changeDescription,
            ChangedAt = DateTime.UtcNow
        };

        _dbContext.ConfigurationChanges.Add(change);
        await _dbContext.SaveChangesAsync();
    }
}
