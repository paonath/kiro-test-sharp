# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- Initial project specification for Puppet Bolt Web Interface
- Requirements document defining core functionality including command execution, inventory management, task/plan execution, authentication, and logging
- Design document outlining ASP.NET Core 8.0 architecture with layered design pattern
- Implementation tasks breakdown covering 25 major tasks from project setup through Docker deployment
- Steering rules for technology stack (ASP.NET Core 8.0, C# 12, Entity Framework Core, SignalR)
- Steering rules for project structure and conventions
- Product overview documentation
- Kiro documentation structure with dedicated `.kiro/` directory for specs, docs, steering rules, and settings
- ASP.NET Core 8.0 Web API project with .NET 8.0 SDK
- Project structure with organized folders: Endpoints, Services, Models, Validators, Data, Middleware, Hubs
- Core configuration in appsettings.json for Bolt, Authentication, Database, Logging, and RateLimiting
- NuGet packages: Entity Framework Core, JWT Authentication, FluentValidation, Serilog, SignalR, Swashbuckle
- Program.cs with JWT authentication, SignalR, CORS, Serilog request logging, and Swagger configuration
- Basic health check endpoint at /health
- README.md with project overview, setup instructions, and configuration guide
- Setup guide documentation in .kiro/docs/
- launchSettings.json for development environment configuration
- .gitkeep files to preserve empty directory structure
- BoltDbContext with DbSets for ExecutionHistory, Users, RefreshTokens, and ConfigurationChanges
- Entity models: ExecutionHistoryEntity with execution tracking and state management
- Entity models: UserEntity with authentication and role-based access control
- Entity models: RefreshTokenEntity with token management and revocation support
- Entity models: ConfigurationChangeEntity for audit trail of configuration changes
- Entity relationships and foreign key constraints (User -> RefreshTokens)
- Database indexes for optimized queries on common access patterns (UserId, StartedAt, State, etc.)
- Enum types for ExecutionType (Command, Task, Plan) and ExecutionState (Queued, Running, Completed, Failed, Cancelled)
- Initial database migration (20251015135933_InitialCreate) with complete schema
- DbContext registration in Program.cs with SQLite configuration
- IAuthenticationService interface with methods for authentication, token refresh, validation, and revocation
- AuthenticationService implementation with JWT token generation and BCrypt password hashing
- JWT access token generation with configurable expiration and claims (user ID, username, role)
- Secure refresh token generation using cryptographic random number generator
- Token refresh flow with automatic revocation of old refresh tokens
- AuthEndpoints with three endpoints: POST /api/auth/login, POST /api/auth/refresh, POST /api/auth/logout
- LoginRequestValidator for validating login credentials
- RefreshTokenRequestValidator for validating refresh token requests
- FluentValidation integration in Program.cs with automatic validator registration
- ErrorResponse model for structured error handling with validation error support
- Comprehensive logging for authentication events (login, token refresh, logout, failures)
- Service registration for IAuthenticationService in Program.cs with scoped lifetime
- Endpoint mapping for AuthEndpoints in Program.cs

### Changed
- Migrated architecture from traditional Controllers to Minimal APIs for improved performance and simplicity
- Replaced data annotation validation with FluentValidation for better separation of concerns
- Updated design document to reflect Minimal API patterns with `Results<T>` return types
- Updated implementation tasks to include FluentValidation validators for all request models
- Enhanced project structure documentation to include `.kiro/` directory organization
- Marked task 1 (project setup and core configuration) as completed in tasks.md
- Marked task 2 (database layer and entities) as completed in tasks.md
- Marked task 3 (authentication and authorization) as completed in tasks.md including all subtasks (3.1, 3.2, 3.3)
- Marked task 4 (process management for Bolt CLI) as completed in tasks.md including all subtasks (4.1, 4.2)
- IProcessManager interface with methods for executing external processes, checking process status, and killing processes
- ProcessManager implementation with real-time stdout/stderr capture via callbacks
- Process execution with configurable timeouts and automatic cleanup on timeout or cancellation
- Concurrent execution limits using SemaphoreSlim to prevent resource exhaustion
- Process tracking using ConcurrentDictionary for managing running processes by execution ID
- ProcessExecutionResult model with comprehensive execution metadata (exit code, output, timing, status flags)
- Graceful process termination with fallback to force kill
- Service registration for IProcessManager in Program.cs with singleton lifetime
- Comprehensive logging for process lifecycle events (start, completion, timeout, cancellation, errors)
- CLAUDE.md developer guidance document with project architecture, commands, patterns, and development workflows
- Claude implementation plan (claude-implementation-plan.md) with detailed 6-phase implementation roadmap
- Claude implementation progress tracker (claude-implementation-progress.md) for monitoring development progress
- BoltModels.cs domain models for Bolt operations including request/response models for commands, tasks, and plans
- CommandExecutionRequest, TaskExecutionRequest, and PlanExecutionRequest models with validation-ready properties
- CommandExecutionResult, TaskExecutionResult, and PlanExecutionResult models with comprehensive execution metadata
- NodeResult model for per-node task execution results
- ExecutionStatus model for tracking ongoing execution state
- BoltTask, BoltTaskDetails, and TaskParameter models for task discovery and metadata
- BoltPlan, BoltPlanDetails, and PlanParameter models for plan discovery and metadata
- ExecutionState enum (Queued, Running, Completed, Failed, Cancelled) matching database entity
- IBoltExecutionService interface defining core Bolt execution operations
- Service interface methods for executing commands, tasks, and plans
- Service interface methods for execution management (status, cancellation)
- Service interface methods for task and plan discovery
- Comprehensive XML documentation for all interface methods with exception specifications
- BoltExecutionService implementation with complete Bolt CLI integration
- Command execution with real-time output capture and database logging
- Task execution with per-node result parsing from JSON output
- Plan execution with return value extraction and logging
- Execution status tracking with in-memory ConcurrentDictionary and database persistence
- Execution cancellation via ProcessManager integration
- Task discovery with ListTasksAsync and GetTaskDetailsAsync methods
- Plan discovery with ListPlansAsync and GetPlanDetailsAsync methods
- JSON parsing for Bolt CLI output (task results, plan return values, metadata)
- ExecutionState conversion helpers between Domain and Entity enums
- Comprehensive error handling with KeyNotFoundException for missing resources
- Database audit logging for all executions with user tracking
- Service registration in Program.cs with Scoped lifetime
- CommandExecutionRequestValidator with security validation for command injection prevention
- TaskExecutionRequestValidator with regex validation for task names and target validation
- PlanExecutionRequestValidator with regex validation for plan names
- Timeout validation (1-3600 seconds) across all execution request validators
- Command injection prevention through unsafe character detection (&&, ||, ;, |, >, <, `, $()
- Target array validation ensuring non-empty values
- Auto-registration of validators via AddValidatorsFromAssemblyContaining

### Changed
- Migrated architecture from traditional Controllers to Minimal APIs for improved performance and simplicity
- Replaced data annotation validation with FluentValidation for better separation of concerns
- Updated design document to reflect Minimal API patterns with `Results<T>` return types
- Updated implementation tasks to include FluentValidation validators for all request models
- Enhanced project structure documentation to include `.kiro/` directory organization
- Marked task 1 (project setup and core configuration) as completed in tasks.md
- Marked task 2 (database layer and entities) as completed in tasks.md
- Marked task 3 (authentication and authorization) as completed in tasks.md including all subtasks (3.1, 3.2, 3.3)
- Marked task 4 (process management for Bolt CLI) as completed in tasks.md including all subtasks (4.1, 4.2)
- Updated global.json to use rollForward "latestMajor" for compatibility with .NET 9.0 SDK

### Removed
- Migration guide moved from specs to docs directory for better organization
