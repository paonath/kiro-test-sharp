using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace BoltWebAPI.Hubs;

/// <summary>
/// SignalR hub for real-time execution updates
/// </summary>
[Authorize]
public class ExecutionHub : Hub
{
    private readonly ILogger<ExecutionHub> _logger;

    public ExecutionHub(ILogger<ExecutionHub> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Called when a client connects to the hub
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var username = Context.User?.Identity?.Name ?? "Unknown";

        _logger.LogInformation(
            "Client connected to ExecutionHub: ConnectionId={ConnectionId}, User={Username}, UserId={UserId}",
            Context.ConnectionId, username, userId);

        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Called when a client disconnects from the hub
    /// </summary>
    /// <param name="exception">Exception if disconnection was due to error</param>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var username = Context.User?.Identity?.Name ?? "Unknown";

        if (exception != null)
        {
            _logger.LogWarning(exception,
                "Client disconnected from ExecutionHub with error: ConnectionId={ConnectionId}, User={Username}, UserId={UserId}",
                Context.ConnectionId, username, userId);
        }
        else
        {
            _logger.LogInformation(
                "Client disconnected from ExecutionHub: ConnectionId={ConnectionId}, User={Username}, UserId={UserId}",
                Context.ConnectionId, username, userId);
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Subscribe to updates for a specific execution
    /// </summary>
    /// <param name="executionId">Execution ID to subscribe to</param>
    public async Task SubscribeToExecution(string executionId)
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var groupName = $"execution_{executionId}";

        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

        _logger.LogInformation(
            "Client subscribed to execution: ExecutionId={ExecutionId}, ConnectionId={ConnectionId}, UserId={UserId}",
            executionId, Context.ConnectionId, userId);

        await Clients.Caller.SendAsync("SubscriptionConfirmed", executionId);
    }

    /// <summary>
    /// Unsubscribe from updates for a specific execution
    /// </summary>
    /// <param name="executionId">Execution ID to unsubscribe from</param>
    public async Task UnsubscribeFromExecution(string executionId)
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var groupName = $"execution_{executionId}";

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);

        _logger.LogInformation(
            "Client unsubscribed from execution: ExecutionId={ExecutionId}, ConnectionId={ConnectionId}, UserId={UserId}",
            executionId, Context.ConnectionId, userId);

        await Clients.Caller.SendAsync("UnsubscriptionConfirmed", executionId);
    }

    /// <summary>
    /// Subscribe to all execution updates for the current user
    /// </summary>
    public async Task SubscribeToUserExecutions()
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning("Cannot subscribe to user executions: UserId not found in claims");
            return;
        }

        var groupName = $"user_{userId}";
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

        _logger.LogInformation(
            "Client subscribed to user executions: UserId={UserId}, ConnectionId={ConnectionId}",
            userId, Context.ConnectionId);

        await Clients.Caller.SendAsync("UserExecutionsSubscriptionConfirmed", userId);
    }

    /// <summary>
    /// Unsubscribe from user execution updates
    /// </summary>
    public async Task UnsubscribeFromUserExecutions()
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            return;
        }

        var groupName = $"user_{userId}";
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);

        _logger.LogInformation(
            "Client unsubscribed from user executions: UserId={UserId}, ConnectionId={ConnectionId}",
            userId, Context.ConnectionId);

        await Clients.Caller.SendAsync("UserExecutionsUnsubscriptionConfirmed", userId);
    }

    /// <summary>
    /// Subscribe to all execution updates (requires admin role)
    /// </summary>
    public async Task SubscribeToAllExecutions()
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var isAdmin = Context.User?.IsInRole("Admin") ?? false;

        if (!isAdmin)
        {
            _logger.LogWarning(
                "Non-admin user attempted to subscribe to all executions: UserId={UserId}",
                userId);

            await Clients.Caller.SendAsync("SubscriptionError", "Admin role required to subscribe to all executions");
            return;
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, "all_executions");

        _logger.LogInformation(
            "Admin subscribed to all executions: UserId={UserId}, ConnectionId={ConnectionId}",
            userId, Context.ConnectionId);

        await Clients.Caller.SendAsync("AllExecutionsSubscriptionConfirmed");
    }

    /// <summary>
    /// Unsubscribe from all execution updates
    /// </summary>
    public async Task UnsubscribeFromAllExecutions()
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "all_executions");

        _logger.LogInformation(
            "Client unsubscribed from all executions: UserId={UserId}, ConnectionId={ConnectionId}",
            userId, Context.ConnectionId);

        await Clients.Caller.SendAsync("AllExecutionsUnsubscriptionConfirmed");
    }

    /// <summary>
    /// Ping endpoint for connection health check
    /// </summary>
    public async Task Ping()
    {
        await Clients.Caller.SendAsync("Pong", DateTime.UtcNow);
    }
}
