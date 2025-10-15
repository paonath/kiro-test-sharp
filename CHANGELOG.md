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

### Changed
- Migrated architecture from traditional Controllers to Minimal APIs for improved performance and simplicity
- Replaced data annotation validation with FluentValidation for better separation of concerns
- Updated design document to reflect Minimal API patterns with `Results<T>` return types
- Updated implementation tasks to include FluentValidation validators for all request models
- Enhanced project structure documentation to include `.kiro/` directory organization

### Removed
- Migration guide moved from specs to docs directory for better organization
