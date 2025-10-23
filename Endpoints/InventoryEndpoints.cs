using System.Security.Claims;
using BoltWebAPI.Models.Domain;
using BoltWebAPI.Services.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;

namespace BoltWebAPI.Endpoints;

/// <summary>
/// Endpoints for Bolt inventory management
/// </summary>
public static class InventoryEndpoints
{
    /// <summary>
    /// Maps all inventory endpoints
    /// </summary>
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/inventory")
            .RequireAuthorization()
            .WithTags("Inventory");

        // Get operations
        group.MapGet("/", GetInventory)
            .WithName("GetInventory")
            .WithSummary("Get complete inventory")
            .WithDescription("Retrieves the complete Bolt inventory structure including all nodes and groups")
            .Produces<Inventory>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
            .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        group.MapGet("/nodes", GetNodes)
            .WithName("GetNodes")
            .WithSummary("Get all nodes")
            .WithDescription("Retrieves all nodes from the Bolt inventory")
            .Produces<IEnumerable<InventoryNode>>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound);

        group.MapGet("/nodes/{nodeUri}", GetNode)
            .WithName("GetNode")
            .WithSummary("Get node details")
            .WithDescription("Retrieves detailed information about a specific node")
            .Produces<InventoryNode>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound);

        group.MapGet("/groups", GetGroups)
            .WithName("GetGroups")
            .WithSummary("Get all groups")
            .WithDescription("Retrieves all groups from the Bolt inventory")
            .Produces<IEnumerable<InventoryGroup>>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound);

        group.MapGet("/groups/{groupName}", GetGroup)
            .WithName("GetGroup")
            .WithSummary("Get group details")
            .WithDescription("Retrieves detailed information about a specific group")
            .Produces<InventoryGroup>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound);

        group.MapGet("/groups/{groupName}/nodes", GetGroupNodes)
            .WithName("GetGroupNodes")
            .WithSummary("Get nodes in group")
            .WithDescription("Retrieves all nodes belonging to a specific group")
            .Produces<IEnumerable<InventoryNode>>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound);

        // Modify operations
        group.MapPost("/nodes", AddNode)
            .WithName("AddNode")
            .WithSummary("Add a new node")
            .WithDescription("Adds a new node to the Bolt inventory")
            .Produces<InventoryNode>(StatusCodes.Status201Created)
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        group.MapPut("/nodes/{nodeUri}", UpdateNode)
            .WithName("UpdateNode")
            .WithSummary("Update a node")
            .WithDescription("Updates an existing node in the Bolt inventory")
            .Produces<InventoryNode>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest);

        group.MapDelete("/nodes/{nodeUri}", DeleteNode)
            .WithName("DeleteNode")
            .WithSummary("Delete a node")
            .WithDescription("Removes a node from the Bolt inventory")
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound);

        group.MapPut("/groups", UpsertGroup)
            .WithName("UpsertGroup")
            .WithSummary("Create or update a group")
            .WithDescription("Creates a new group or updates an existing group in the Bolt inventory")
            .Produces<InventoryGroup>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest);

        group.MapDelete("/groups/{groupName}", DeleteGroup)
            .WithName("DeleteGroup")
            .WithSummary("Delete a group")
            .WithDescription("Removes a group from the Bolt inventory")
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound);

        group.MapPut("/", UpdateInventory)
            .WithName("UpdateInventory")
            .WithSummary("Update entire inventory")
            .WithDescription("Updates the entire Bolt inventory from YAML content")
            .Produces<Inventory>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest);

        group.MapPost("/validate", ValidateInventory)
            .WithName("ValidateInventory")
            .WithSummary("Validate inventory YAML")
            .WithDescription("Validates inventory YAML content without saving")
            .Produces<ConfigurationValidationResult>(StatusCodes.Status200OK);
    }

    private static async Task<Results<Ok<Inventory>, NotFound<ErrorResponse>, ProblemHttpResult>>
        GetInventory(
            IInventoryService inventoryService,
            ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("InventoryEndpoints");

        try
        {
            var inventory = await inventoryService.GetInventoryAsync();
            return TypedResults.Ok(inventory);
        }
        catch (FileNotFoundException ex)
        {
            logger.LogWarning("Inventory file not found: {Message}", ex.Message);
            return TypedResults.NotFound(new ErrorResponse
            {
                Message = "Inventory file not found",
                ErrorCode = "INVENTORY_NOT_FOUND"
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving inventory");
            return TypedResults.Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Error Retrieving Inventory",
                detail: ex.Message);
        }
    }

    private static async Task<Ok<IEnumerable<InventoryNode>>>
        GetNodes(IInventoryService inventoryService)
    {
        var nodes = await inventoryService.GetNodesAsync();
        return TypedResults.Ok(nodes);
    }

    private static async Task<Results<Ok<InventoryNode>, NotFound<ErrorResponse>>>
        GetNode(
            string nodeUri,
            IInventoryService inventoryService,
            ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("InventoryEndpoints");

        try
        {
            var node = await inventoryService.GetNodeAsync(nodeUri);
            return TypedResults.Ok(node);
        }
        catch (KeyNotFoundException)
        {
            logger.LogWarning("Node not found: {NodeUri}", nodeUri);
            return TypedResults.NotFound(new ErrorResponse
            {
                Message = $"Node '{nodeUri}' not found",
                ErrorCode = "NODE_NOT_FOUND"
            });
        }
    }

    private static async Task<Ok<IEnumerable<InventoryGroup>>>
        GetGroups(IInventoryService inventoryService)
    {
        var groups = await inventoryService.GetGroupsAsync();
        return TypedResults.Ok(groups);
    }

    private static async Task<Results<Ok<InventoryGroup>, NotFound<ErrorResponse>>>
        GetGroup(
            string groupName,
            IInventoryService inventoryService,
            ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("InventoryEndpoints");

        try
        {
            var group = await inventoryService.GetGroupAsync(groupName);
            return TypedResults.Ok(group);
        }
        catch (KeyNotFoundException)
        {
            logger.LogWarning("Group not found: {GroupName}", groupName);
            return TypedResults.NotFound(new ErrorResponse
            {
                Message = $"Group '{groupName}' not found",
                ErrorCode = "GROUP_NOT_FOUND"
            });
        }
    }

    private static async Task<Results<Ok<IEnumerable<InventoryNode>>, NotFound<ErrorResponse>>>
        GetGroupNodes(
            string groupName,
            IInventoryService inventoryService,
            ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("InventoryEndpoints");

        try
        {
            var nodes = await inventoryService.GetGroupNodesAsync(groupName);
            return TypedResults.Ok(nodes);
        }
        catch (KeyNotFoundException)
        {
            logger.LogWarning("Group not found: {GroupName}", groupName);
            return TypedResults.NotFound(new ErrorResponse
            {
                Message = $"Group '{groupName}' not found",
                ErrorCode = "GROUP_NOT_FOUND"
            });
        }
    }

    private static async Task<Results<Created<InventoryNode>, BadRequest<ErrorResponse>, ProblemHttpResult>>
        AddNode(
            AddNodeRequest request,
            IValidator<AddNodeRequest> validator,
            IInventoryService inventoryService,
            ClaimsPrincipal user,
            ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("InventoryEndpoints");

        var validationResult = await validator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

            return TypedResults.BadRequest(new ErrorResponse
            {
                Message = "Validation failed",
                ErrorCode = "VALIDATION_ERROR",
                ValidationErrors = errors
            });
        }

        try
        {
            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown";
            var node = await inventoryService.AddNodeAsync(request, userId);

            logger.LogInformation("Added node {NodeUri} (User: {UserId})", request.Uri, userId);

            return TypedResults.Created($"/api/inventory/nodes/{node.Uri}", node);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Invalid add node request");
            return TypedResults.BadRequest(new ErrorResponse
            {
                Message = ex.Message,
                ErrorCode = "INVALID_REQUEST"
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error adding node");
            return TypedResults.Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Error Adding Node",
                detail: ex.Message);
        }
    }

    private static async Task<Results<Ok<InventoryNode>, NotFound<ErrorResponse>, BadRequest<ErrorResponse>>>
        UpdateNode(
            string nodeUri,
            AddNodeRequest request,
            IValidator<AddNodeRequest> validator,
            IInventoryService inventoryService,
            ClaimsPrincipal user,
            ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("InventoryEndpoints");

        var validationResult = await validator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

            return TypedResults.BadRequest(new ErrorResponse
            {
                Message = "Validation failed",
                ErrorCode = "VALIDATION_ERROR",
                ValidationErrors = errors
            });
        }

        try
        {
            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown";
            var node = await inventoryService.UpdateNodeAsync(nodeUri, request, userId);

            logger.LogInformation("Updated node {NodeUri} (User: {UserId})", nodeUri, userId);

            return TypedResults.Ok(node);
        }
        catch (KeyNotFoundException)
        {
            logger.LogWarning("Node not found for update: {NodeUri}", nodeUri);
            return TypedResults.NotFound(new ErrorResponse
            {
                Message = $"Node '{nodeUri}' not found",
                ErrorCode = "NODE_NOT_FOUND"
            });
        }
    }

    private static async Task<Results<NoContent, NotFound<ErrorResponse>>>
        DeleteNode(
            string nodeUri,
            IInventoryService inventoryService,
            ClaimsPrincipal user,
            ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("InventoryEndpoints");

        try
        {
            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown";
            await inventoryService.DeleteNodeAsync(nodeUri, userId);

            logger.LogInformation("Deleted node {NodeUri} (User: {UserId})", nodeUri, userId);

            return TypedResults.NoContent();
        }
        catch (KeyNotFoundException)
        {
            logger.LogWarning("Node not found for deletion: {NodeUri}", nodeUri);
            return TypedResults.NotFound(new ErrorResponse
            {
                Message = $"Node '{nodeUri}' not found",
                ErrorCode = "NODE_NOT_FOUND"
            });
        }
    }

    private static async Task<Results<Ok<InventoryGroup>, BadRequest<ErrorResponse>>>
        UpsertGroup(
            UpsertGroupRequest request,
            IValidator<UpsertGroupRequest> validator,
            IInventoryService inventoryService,
            ClaimsPrincipal user,
            ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("InventoryEndpoints");

        var validationResult = await validator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

            return TypedResults.BadRequest(new ErrorResponse
            {
                Message = "Validation failed",
                ErrorCode = "VALIDATION_ERROR",
                ValidationErrors = errors
            });
        }

        try
        {
            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown";
            var group = await inventoryService.UpsertGroupAsync(request, userId);

            logger.LogInformation("Upserted group {GroupName} (User: {UserId})", request.Name, userId);

            return TypedResults.Ok(group);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Invalid upsert group request");
            return TypedResults.BadRequest(new ErrorResponse
            {
                Message = ex.Message,
                ErrorCode = "INVALID_REQUEST"
            });
        }
    }

    private static async Task<Results<NoContent, NotFound<ErrorResponse>>>
        DeleteGroup(
            string groupName,
            IInventoryService inventoryService,
            ClaimsPrincipal user,
            ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("InventoryEndpoints");

        try
        {
            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown";
            await inventoryService.DeleteGroupAsync(groupName, userId);

            logger.LogInformation("Deleted group {GroupName} (User: {UserId})", groupName, userId);

            return TypedResults.NoContent();
        }
        catch (KeyNotFoundException)
        {
            logger.LogWarning("Group not found for deletion: {GroupName}", groupName);
            return TypedResults.NotFound(new ErrorResponse
            {
                Message = $"Group '{groupName}' not found",
                ErrorCode = "GROUP_NOT_FOUND"
            });
        }
    }

    private static async Task<Results<Ok<Inventory>, BadRequest<ErrorResponse>>>
        UpdateInventory(
            UpdateInventoryRequest request,
            IValidator<UpdateInventoryRequest> validator,
            IInventoryService inventoryService,
            ClaimsPrincipal user,
            ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("InventoryEndpoints");

        var validationResult = await validator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

            return TypedResults.BadRequest(new ErrorResponse
            {
                Message = "Validation failed",
                ErrorCode = "VALIDATION_ERROR",
                ValidationErrors = errors
            });
        }

        try
        {
            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown";
            var inventory = await inventoryService.UpdateInventoryAsync(request, userId);

            logger.LogInformation("Updated inventory (User: {UserId})", userId);

            return TypedResults.Ok(inventory);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Invalid update inventory request");
            return TypedResults.BadRequest(new ErrorResponse
            {
                Message = ex.Message,
                ErrorCode = "INVALID_REQUEST"
            });
        }
    }

    private static async Task<Ok<ConfigurationValidationResult>>
        ValidateInventory(
            UpdateInventoryRequest request,
            IInventoryService inventoryService)
    {
        var result = await inventoryService.ValidateInventoryAsync(request.YamlContent);
        return TypedResults.Ok(result);
    }
}
