# Implementation Plan

- [ ] 1. Set up project structure and core configuration
  - Create ASP.NET Core Web API project with .NET 8.0
  - Configure project structure with folders: Controllers, Services, Models, Data, Middleware
  - Set up appsettings.json with Bolt, Authentication, Database, and Logging configuration
  - Add required NuGet packages: Entity Framework Core, JWT Authentication, Serilog, SignalR
  - _Requirements: 1.1, 5.1_

- [ ] 2. Implement database layer and entities
  - Create DbContext class with DbSets for ExecutionHistory, User, RefreshToken, ConfigurationChange
  - Define entity models: ExecutionHistoryEntity, UserEntity, RefreshTokenEntity, ConfigurationChangeEntity
  - Configure entity relationships and indexes
  - Create initial database migration
  - _Requirements: 6.1, 5.1_

- [ ] 3. Implement authentication and authorization
  - [ ] 3.1 Create authentication service and JWT token generation
    - Implement IAuthenticationService with login, token refresh, and validation methods
    - Create JWT token generation logic with configurable expiration
    - Implement password hashing using BCrypt
    - _Requirements: 5.1, 5.2_
  
  - [ ] 3.2 Create AuthController for login and token management
    - Implement POST /api/auth/login endpoint
    - Implement POST /api/auth/refresh endpoint for token refresh
    - Implement POST /api/auth/logout endpoint
    - _Requirements: 5.1, 5.2_
  
  - [ ] 3.3 Configure JWT authentication middleware
    - Add JWT bearer authentication to the pipeline
    - Configure token validation parameters
    - Set up role-based authorization policies
    - _Requirements: 5.3, 5.4, 5.5_

- [ ] 4. Implement process management for Bolt CLI
  - [ ] 4.1 Create IProcessManager interface and implementation
    - Implement ExecuteAsync method using System.Diagnostics.Process
    - Capture stdout and stderr with real-time callbacks
    - Implement process tracking by execution ID
    - Handle process cancellation and cleanup
    - _Requirements: 1.1, 1.2, 1.5_
  
  - [ ] 4.2 Add timeout and resource management
    - Implement configurable timeout for process execution
    - Add concurrent execution limits
    - Implement process cleanup on timeout or cancellation
    - _Requirements: 1.5, 8.4_

- [ ] 5. Implement core Bolt execution service
  - [ ] 5.1 Create IBoltExecutionService interface and implementation
    - Implement ExecuteCommandAsync for arbitrary Bolt commands
    - Implement ExecuteTaskAsync for Bolt tasks
    - Implement ExecutePlanAsync for Bolt plans
    - Build Bolt CLI command strings with proper argument escaping
    - _Requirements: 1.1, 3.2, 4.2_
  
  - [ ] 5.2 Add execution tracking and status management
    - Implement GetExecutionStatusAsync to query running executions
    - Implement CancelExecutionAsync to terminate running processes
    - Store execution state in memory (ConcurrentDictionary)
    - _Requirements: 8.1, 8.2, 8.4_
  
  - [ ] 5.3 Parse and structure Bolt CLI output
    - Parse JSON output from Bolt commands
    - Extract node results from task/plan executions
    - Handle error messages and exit codes
    - _Requirements: 1.2, 1.3, 1.4, 3.3, 3.5_

- [ ] 6. Implement execution history service
  - Create IExecutionHistoryService interface and implementation
  - Implement LogExecutionAsync to persist execution records
  - Implement GetHistoryAsync with pagination and date filtering
  - Implement GetExecutionDetailsAsync for detailed execution info
  - _Requirements: 6.1, 6.2, 6.3, 6.4, 6.5_

- [ ] 7. Implement inventory service
  - Create IInventoryService interface and implementation
  - Implement GetInventoryAsync to parse inventory.yaml file
  - Implement GetGroupsAsync to list inventory groups
  - Implement GetNodesByGroupAsync to filter nodes by group
  - Add inventory validation logic
  - _Requirements: 2.1, 2.2, 2.3, 2.4_

- [ ] 8. Implement configuration service
  - Create IConfigurationService interface and implementation
  - Implement GetConfigurationAsync to read bolt-project.yaml
  - Implement UpdateConfigurationAsync to write configuration changes
  - Implement ValidateConfigurationAsync for configuration validation
  - Log configuration changes with user information
  - _Requirements: 7.1, 7.2, 7.3, 7.4, 7.5_

- [ ] 9. Implement BoltCommandController
  - Create controller with [Authorize] attribute
  - Implement POST /api/bolt/commands/execute endpoint
  - Implement GET /api/bolt/commands/{executionId}/status endpoint
  - Implement POST /api/bolt/commands/{executionId}/cancel endpoint
  - Add input validation and error handling
  - _Requirements: 1.1, 1.2, 1.3, 1.4, 8.2, 8.4_

- [ ] 10. Implement BoltTaskController
  - Create controller with [Authorize] attribute
  - Implement GET /api/bolt/tasks endpoint to list tasks
  - Implement GET /api/bolt/tasks/{taskName} endpoint for task details
  - Implement POST /api/bolt/tasks/execute endpoint
  - Parse task metadata from Bolt CLI
  - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5_

- [ ] 11. Implement BoltPlanController
  - Create controller with [Authorize] attribute
  - Implement GET /api/bolt/plans endpoint to list plans
  - Implement GET /api/bolt/plans/{planName} endpoint for plan details
  - Implement POST /api/bolt/plans/execute endpoint
  - Parse plan metadata from Bolt CLI
  - _Requirements: 4.1, 4.2, 4.3, 4.4, 4.5_

- [ ] 12. Implement InventoryController
  - Create controller with [Authorize] attribute
  - Implement GET /api/bolt/inventory endpoint
  - Implement GET /api/bolt/inventory/groups endpoint
  - Implement GET /api/bolt/inventory/groups/{groupName}/nodes endpoint
  - Add error handling for invalid inventory files
  - _Requirements: 2.1, 2.2, 2.3, 2.4_

- [ ] 13. Implement ConfigurationController
  - Create controller with [Authorize(Roles = "Admin")] attribute
  - Implement GET /api/bolt/config endpoint
  - Implement PUT /api/bolt/config endpoint
  - Implement POST /api/bolt/config/validate endpoint
  - Add validation and error handling
  - _Requirements: 7.1, 7.2, 7.3, 7.4, 7.5_

- [ ] 14. Implement HistoryController
  - Create controller with [Authorize] attribute
  - Implement GET /api/bolt/history endpoint with pagination
  - Add query parameters for date range filtering
  - Implement GET /api/bolt/history/{executionId} endpoint
  - Return detailed execution information including output
  - _Requirements: 6.1, 6.2, 6.3, 6.4, 6.5_

- [ ] 15. Implement SignalR hub for real-time updates
  - [ ] 15.1 Create ExecutionHub with authorization
    - Implement SubscribeToExecution method
    - Implement UnsubscribeFromExecution method
    - Add connection management
    - _Requirements: 8.1, 8.2_
  
  - [ ] 15.2 Integrate SignalR with execution service
    - Send OnOutputReceived events during execution
    - Send OnExecutionCompleted events on success
    - Send OnExecutionFailed events on failure
    - Stream output in real-time as it's generated
    - _Requirements: 8.2, 8.3_

- [ ] 16. Implement global error handling middleware
  - Create exception handling middleware
  - Map exceptions to appropriate HTTP status codes
  - Return structured ErrorResponse model
  - Log all exceptions with trace IDs
  - Sanitize error messages for production
  - _Requirements: 1.4, 2.3, 4.4, 7.3_

- [ ] 17. Add input validation and security
  - [ ] 17.1 Implement request validation
    - Add data annotations to request models
    - Validate command arguments for injection attacks
    - Validate file paths to prevent directory traversal
    - Add model validation middleware
    - _Requirements: 1.1, 3.2, 4.2, 7.2_
  
  - [ ] 17.2 Configure security middleware
    - Add HTTPS redirection
    - Configure CORS for frontend origin
    - Add rate limiting middleware
    - Set request size limits
    - _Requirements: 5.1, 5.3_

- [ ] 18. Configure logging with Serilog
  - Set up Serilog with file and console sinks
  - Configure structured logging
  - Add request logging middleware
  - Log all Bolt command executions
  - Configure log rotation and retention
  - _Requirements: 6.1, 7.5_

- [ ] 19. Add API documentation with Swagger
  - Configure Swagger/OpenAPI generation
  - Add XML documentation comments to controllers
  - Configure JWT authentication in Swagger UI
  - Add example requests and responses
  - _Requirements: All endpoints_

- [ ] 20. Create health check and monitoring endpoints
  - Implement /health endpoint with database check
  - Implement /health/ready endpoint for readiness probe
  - Add metrics endpoint for Prometheus (optional)
  - Check Bolt CLI availability in health check
  - _Requirements: All_

- [ ] 21. Set up dependency injection and service registration
  - Register all services in Program.cs
  - Configure service lifetimes (Scoped, Singleton, Transient)
  - Register DbContext with connection string
  - Configure authentication and authorization services
  - Register SignalR services
  - _Requirements: All_

- [ ] 22. Create database seeding for initial data
  - Create default admin user
  - Seed initial configuration if needed
  - Create database initialization logic
  - Add migration application on startup (development only)
  - _Requirements: 5.1_

- [ ]* 23. Write integration tests for API endpoints
  - Set up WebApplicationFactory for testing
  - Write tests for authentication flow
  - Write tests for command execution endpoints
  - Write tests for inventory and configuration endpoints
  - Use in-memory database for testing
  - _Requirements: All_

- [ ] 24. Create Docker support
  - Create Dockerfile with .NET 8.0 runtime
  - Install Puppet Bolt in container image
  - Configure environment variables
  - Create docker-compose.yml for local development
  - Add volume mounts for Bolt configuration
  - _Requirements: All_

- [ ] 25. Add configuration validation on startup
  - Validate Bolt executable path exists
  - Validate required configuration files exist
  - Check database connectivity
  - Validate JWT secret is configured
  - Log configuration status on startup
  - _Requirements: 7.1, 7.3_
