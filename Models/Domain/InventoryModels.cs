namespace BoltWebAPI.Models.Domain;

/// <summary>
/// Represents a single node in the Bolt inventory
/// </summary>
public class InventoryNode
{
    /// <summary>
    /// Node identifier (URI or hostname)
    /// </summary>
    public string Uri { get; set; } = string.Empty;

    /// <summary>
    /// Node name/alias
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Groups this node belongs to
    /// </summary>
    public List<string> Groups { get; set; } = new();

    /// <summary>
    /// Node-specific configuration
    /// </summary>
    public Dictionary<string, object> Config { get; set; } = new();

    /// <summary>
    /// Node facts (gathered information)
    /// </summary>
    public Dictionary<string, object> Facts { get; set; } = new();

    /// <summary>
    /// Node variables
    /// </summary>
    public Dictionary<string, object> Vars { get; set; } = new();

    /// <summary>
    /// Node features (capabilities)
    /// </summary>
    public List<string> Features { get; set; } = new();

    /// <summary>
    /// Plugin hooks configuration
    /// </summary>
    public Dictionary<string, object> PluginHooks { get; set; } = new();
}

/// <summary>
/// Represents a group of nodes in the Bolt inventory
/// </summary>
public class InventoryGroup
{
    /// <summary>
    /// Group name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Nodes in this group (URIs)
    /// </summary>
    public List<string> Nodes { get; set; } = new();

    /// <summary>
    /// Nested groups
    /// </summary>
    public List<string> Groups { get; set; } = new();

    /// <summary>
    /// Group-level configuration
    /// </summary>
    public Dictionary<string, object> Config { get; set; } = new();

    /// <summary>
    /// Group-level variables
    /// </summary>
    public Dictionary<string, object> Vars { get; set; } = new();

    /// <summary>
    /// Group-level facts
    /// </summary>
    public Dictionary<string, object> Facts { get; set; } = new();
}

/// <summary>
/// Complete inventory structure
/// </summary>
public class Inventory
{
    /// <summary>
    /// Inventory format version
    /// </summary>
    public int Version { get; set; } = 2;

    /// <summary>
    /// All nodes in the inventory
    /// </summary>
    public List<InventoryNode> Nodes { get; set; } = new();

    /// <summary>
    /// All groups in the inventory
    /// </summary>
    public List<InventoryGroup> Groups { get; set; } = new();

    /// <summary>
    /// Global configuration
    /// </summary>
    public Dictionary<string, object> Config { get; set; } = new();
}

/// <summary>
/// Request to update inventory configuration
/// </summary>
public class UpdateInventoryRequest
{
    /// <summary>
    /// Inventory YAML content
    /// </summary>
    public string YamlContent { get; set; } = string.Empty;

    /// <summary>
    /// Optional validation only (don't save)
    /// </summary>
    public bool ValidateOnly { get; set; } = false;
}

/// <summary>
/// Request to add a node to inventory
/// </summary>
public class AddNodeRequest
{
    /// <summary>
    /// Node URI or hostname
    /// </summary>
    public string Uri { get; set; } = string.Empty;

    /// <summary>
    /// Node name/alias
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Groups to add the node to
    /// </summary>
    public List<string> Groups { get; set; } = new();

    /// <summary>
    /// Node configuration
    /// </summary>
    public Dictionary<string, object> Config { get; set; } = new();
}

/// <summary>
/// Request to create or update a group
/// </summary>
public class UpsertGroupRequest
{
    /// <summary>
    /// Group name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Nodes to include (URIs)
    /// </summary>
    public List<string> Nodes { get; set; } = new();

    /// <summary>
    /// Nested groups
    /// </summary>
    public List<string> Groups { get; set; } = new();

    /// <summary>
    /// Group configuration
    /// </summary>
    public Dictionary<string, object> Config { get; set; } = new();
}
