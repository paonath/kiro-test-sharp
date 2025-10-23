using BoltWebAPI.Models.Domain;

namespace BoltWebAPI.Services.Interfaces;

/// <summary>
/// Service for managing Bolt inventory
/// </summary>
public interface IInventoryService
{
    /// <summary>
    /// Gets the complete inventory
    /// </summary>
    /// <returns>Complete inventory structure</returns>
    /// <exception cref="FileNotFoundException">Thrown when inventory file is not found</exception>
    /// <exception cref="InvalidOperationException">Thrown when inventory cannot be parsed</exception>
    Task<Inventory> GetInventoryAsync();

    /// <summary>
    /// Gets all nodes from inventory
    /// </summary>
    /// <returns>List of inventory nodes</returns>
    /// <exception cref="FileNotFoundException">Thrown when inventory file is not found</exception>
    /// <exception cref="InvalidOperationException">Thrown when inventory cannot be parsed</exception>
    Task<IEnumerable<InventoryNode>> GetNodesAsync();

    /// <summary>
    /// Gets a specific node by URI
    /// </summary>
    /// <param name="nodeUri">Node URI or hostname</param>
    /// <returns>Inventory node</returns>
    /// <exception cref="KeyNotFoundException">Thrown when node is not found</exception>
    /// <exception cref="FileNotFoundException">Thrown when inventory file is not found</exception>
    /// <exception cref="InvalidOperationException">Thrown when inventory cannot be parsed</exception>
    Task<InventoryNode> GetNodeAsync(string nodeUri);

    /// <summary>
    /// Gets all groups from inventory
    /// </summary>
    /// <returns>List of inventory groups</returns>
    /// <exception cref="FileNotFoundException">Thrown when inventory file is not found</exception>
    /// <exception cref="InvalidOperationException">Thrown when inventory cannot be parsed</exception>
    Task<IEnumerable<InventoryGroup>> GetGroupsAsync();

    /// <summary>
    /// Gets a specific group by name
    /// </summary>
    /// <param name="groupName">Group name</param>
    /// <returns>Inventory group</returns>
    /// <exception cref="KeyNotFoundException">Thrown when group is not found</exception>
    /// <exception cref="FileNotFoundException">Thrown when inventory file is not found</exception>
    /// <exception cref="InvalidOperationException">Thrown when inventory cannot be parsed</exception>
    Task<InventoryGroup> GetGroupAsync(string groupName);

    /// <summary>
    /// Gets nodes belonging to a specific group
    /// </summary>
    /// <param name="groupName">Group name</param>
    /// <returns>List of nodes in the group</returns>
    /// <exception cref="KeyNotFoundException">Thrown when group is not found</exception>
    /// <exception cref="FileNotFoundException">Thrown when inventory file is not found</exception>
    /// <exception cref="InvalidOperationException">Thrown when inventory cannot be parsed</exception>
    Task<IEnumerable<InventoryNode>> GetGroupNodesAsync(string groupName);

    /// <summary>
    /// Adds a new node to inventory
    /// </summary>
    /// <param name="request">Add node request</param>
    /// <param name="userId">User performing the operation</param>
    /// <returns>Created inventory node</returns>
    /// <exception cref="ArgumentException">Thrown when node already exists or request is invalid</exception>
    /// <exception cref="InvalidOperationException">Thrown when inventory cannot be updated</exception>
    Task<InventoryNode> AddNodeAsync(AddNodeRequest request, string userId);

    /// <summary>
    /// Updates an existing node in inventory
    /// </summary>
    /// <param name="nodeUri">Node URI to update</param>
    /// <param name="request">Update node request</param>
    /// <param name="userId">User performing the operation</param>
    /// <returns>Updated inventory node</returns>
    /// <exception cref="KeyNotFoundException">Thrown when node is not found</exception>
    /// <exception cref="InvalidOperationException">Thrown when inventory cannot be updated</exception>
    Task<InventoryNode> UpdateNodeAsync(string nodeUri, AddNodeRequest request, string userId);

    /// <summary>
    /// Removes a node from inventory
    /// </summary>
    /// <param name="nodeUri">Node URI to remove</param>
    /// <param name="userId">User performing the operation</param>
    /// <exception cref="KeyNotFoundException">Thrown when node is not found</exception>
    /// <exception cref="InvalidOperationException">Thrown when inventory cannot be updated</exception>
    Task DeleteNodeAsync(string nodeUri, string userId);

    /// <summary>
    /// Creates or updates a group in inventory
    /// </summary>
    /// <param name="request">Upsert group request</param>
    /// <param name="userId">User performing the operation</param>
    /// <returns>Created or updated inventory group</returns>
    /// <exception cref="InvalidOperationException">Thrown when inventory cannot be updated</exception>
    Task<InventoryGroup> UpsertGroupAsync(UpsertGroupRequest request, string userId);

    /// <summary>
    /// Removes a group from inventory
    /// </summary>
    /// <param name="groupName">Group name to remove</param>
    /// <param name="userId">User performing the operation</param>
    /// <exception cref="KeyNotFoundException">Thrown when group is not found</exception>
    /// <exception cref="InvalidOperationException">Thrown when inventory cannot be updated</exception>
    Task DeleteGroupAsync(string groupName, string userId);

    /// <summary>
    /// Updates the entire inventory from YAML content
    /// </summary>
    /// <param name="request">Update inventory request</param>
    /// <param name="userId">User performing the operation</param>
    /// <returns>Updated inventory structure</returns>
    /// <exception cref="ArgumentException">Thrown when YAML is invalid</exception>
    /// <exception cref="InvalidOperationException">Thrown when inventory cannot be updated</exception>
    Task<Inventory> UpdateInventoryAsync(UpdateInventoryRequest request, string userId);

    /// <summary>
    /// Validates inventory YAML content without saving
    /// </summary>
    /// <param name="yamlContent">YAML content to validate</param>
    /// <returns>Validation result</returns>
    Task<ConfigurationValidationResult> ValidateInventoryAsync(string yamlContent);
}
