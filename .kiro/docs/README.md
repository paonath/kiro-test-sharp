# Kiro Documentation

This directory contains general Kiro-related documentation for the project.

## Purpose

The `.kiro/docs/` directory is for documentation that supports the development workflow but is not part of the application itself. This includes:

- Architecture decision records (ADRs)
- Migration guides and upgrade documentation
- Development workflow guides
- Technical design discussions
- Kiro-specific how-to guides
- Project conventions and best practices documentation

## Directory Structure

```
.kiro/
├── docs/              # General documentation (you are here)
├── specs/             # Feature specifications with requirements, design, and tasks
├── steering/          # Active steering rules that guide Kiro's behavior
├── settings/          # Kiro IDE settings (MCP configuration, etc.)
└── hooks/             # Agent hooks for automated workflows
```

## What Goes Here vs. Other Directories

### `.kiro/docs/` - General Documentation
- Migration guides (e.g., MIGRATION_TO_MINIMAL_API.md)
- Architecture decision records
- Development setup guides
- Technical discussions and proposals
- Historical documentation

### `.kiro/specs/` - Feature Specifications
- Structured feature specs with requirements, design, and tasks
- Active implementation plans
- Feature-specific design documents

### `.kiro/steering/` - Steering Rules
- Active conventions that guide development
- Technology stack definitions
- Project structure rules
- Coding standards

### Application Root - Application Documentation
- README.md - Project overview for end users
- API documentation
- User guides
- Deployment documentation

## Convention

All Kiro-related files MUST be placed under `.kiro/` to:
- Keep the project root clean
- Separate development workflow from application code
- Make it easy to exclude from production builds
- Maintain clear boundaries between app docs and dev docs
