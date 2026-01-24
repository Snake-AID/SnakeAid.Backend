# SnakeAid Backend Documentation

> Comprehensive documentation for SnakeAid Backend API

## Welcome! 🐍

This documentation site provides detailed information about the SnakeAid Backend implementation, including:

- **ASP.NET Identity**: Authentication and authorization system
- **API Endpoints**: RESTful API documentation
- **Database Schema**: Entity models and relationships
- **Development Guide**: How to contribute and maintain the codebase

## Quick Links

- [ASP.NET Identity Introduction](ASP%20Identity/aspnet-identity.introduction.md)
- [Source Code Reference](ASP%20Identity/aspnet-identity.sourcecode.md)
- [API Usage Guide](ASP%20Identity/aspnet-identity.usageguilde.md)
- [Documentation Organization Guide](README.md)

## Documentation Structure

Each feature/technology has **5 types of documentation**:

| File Type | Purpose | Audience |
|-----------|---------|----------|
| `*.introduction.md` | Overview and requirements | PM, Tech Lead, New Developers |
| `*.plan.md` | Implementation plan | Developers, Tech Lead |
| `*.prompt.md` | AI Agent instructions | AI Agents |
| `*.sourcecode.md` | Code reference (function-level) | AI Agents, Developers |
| `*.usageguilde.md` | API usage examples | Frontend Developers |

## Features

This documentation site includes:

- 🔍 **Full-text search** - Search across all documentation
- 📱 **Responsive design** - Works on mobile and desktop
- 🎨 **Syntax highlighting** - C#, JSON, SQL, JavaScript, TypeScript
- 📊 **Mermaid diagrams** - Flow charts and diagrams
- 📋 **Copy code** - One-click code copying
- 🔗 **Deep linking** - Share links to specific sections

## Getting Started

### For Backend Developers
1. Read [Documentation Organization Guide](README.md)
2. Check [ASP.NET Identity Source Code](ASP%20Identity/aspnet-identity.sourcecode.md)
3. Follow the implementation patterns

### For Frontend Developers
1. Read [API Usage Guide](ASP%20Identity/aspnet-identity.usageguilde.md)
2. Check authentication flow examples
3. Integrate with your frontend app

### For AI Agents
1. Read `*.prompt.md` for implementation instructions
2. Use `*.sourcecode.md` as context (saves tokens!)
3. Generate code following existing patterns

## Tech Stack

- **Framework**: ASP.NET Core 8.0
- **Database**: PostgreSQL
- **ORM**: Entity Framework Core
- **Authentication**: ASP.NET Core Identity + JWT
- **External Auth**: Google Sign-In

## Contributing

When adding new features:

1. Create folder: `docs/[Feature Name]/`
2. Create 5 documentation files following the naming convention
3. Update `_sidebar.md` to add navigation links
4. Follow the documentation guidelines in [README.md](README.md)

---

**Project**: SnakeAid Backend  
**Last Updated**: 2026-01-24  
**Maintained By**: Backend Team
