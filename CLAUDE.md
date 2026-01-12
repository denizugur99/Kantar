# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Kantarv2 is an ASP.NET Core Web API (.NET 10.0) implementing a product price management system with JWT authentication, user management, and LLM integration. The application follows CQRS pattern using MediatR and integrates with PostgreSQL, Redis, Elasticsearch, and external LLM services.

## Build and Run Commands

```bash
# Build the project
dotnet build

# Run the application
dotnet run --project Kantarv2/Kantarv2.csproj

# Restore dependencies
dotnet restore

# Create a new migration
dotnet ef migrations add MigrationName --project Kantarv2

# Apply migrations to database
dotnet ef database update --project Kantarv2

# Clean build artifacts
dotnet clean
```

## Architecture

### CQRS Pattern with MediatR

The application strictly separates read (Query) and write (Command) operations:

- **Commands**: Located in `Command/` folders (User, Product, UnitPrice), handled by `Handler/CommandHandler/`
- **Queries**: Located in `Queries/` folders (User, Products, UnitPrice, Llm), handled by `Handler/QueryHandler/`
- Each command/query implements `IRequest<Response<T>>` from MediatR
- Handlers implement `IRequestHandler<TRequest, TResponse>`

Example command: [LoginCommand.cs](Kantarv2/Command/User/LoginCommand.cs) handled by [UserCommandHandler.cs](Kantarv2/Handler/CommandHandler/UserCommandHandler.cs)

### Response Pattern

All handlers return `Response<T>` from [Dtos/Response.cs](Kantarv2/Dtos/Response.cs) which includes:
- `Data`: Generic payload
- `StatusCode`: HTTP status code
- `IsSuccess`: Success indicator
- `Errors`: Error messages
- `Pagination`: Optional pagination metadata

Use static factory methods: `Response<T>.Success()`, `Response<T>.Fail()`

### Database Context

[DAL/KantarDbContext.cs](Kantarv2/DAL/KantarDbContext.cs) inherits from `IdentityDbContext<User, IdentityRole<int>, int>`:
- Primary entities: User (Identity), Product, UnitPrice
- Uses PostgreSQL with Entity Framework Core
- Custom DateTime converter for UTC handling on RefreshTokenExpireDate
- Connection string: `DefaultConnection` in appsettings.json

### Authentication & Authorization

**JWT-based authentication with custom SecurityStamp validation:**
- Token service: [Services/TokenService.cs](Kantarv2/Services/TokenService.cs)
- Tokens configured in appsettings.json under `Appsettings:Token`, `Issuer`, `Audience`
- SecurityStamp validation occurs on every request via JWT bearer events in [Program.cs](Kantarv2/Program.cs:90-121)
- Validates user exists, is not deleted, and SecurityStamp matches
- Role-based authorization with auto-seeding via [RoleSeeder.cs](Kantarv2/Services/RoleSeeder.cs)

**Authentication flow:**
1. User logs in → receives AccessToken + RefreshToken
2. AccessToken includes SecurityStamp claim
3. On each request, JWT middleware validates SecurityStamp against database
4. Logout invalidates tokens by updating user's SecurityStamp

### Middleware Pipeline Order

From [Program.cs](Kantarv2/Program.cs:142-149):
1. `UseHttpsRedirection()`
2. `UseAuthentication()` - JWT validation
3. `UserContextMiddleware` - Enriches Serilog context with user claims (UserId, UserName, Email, Roles)
4. `UseAuthorization()`

**Important**: UserContextMiddleware must come after UseAuthentication() to access authenticated user claims.

### Logging with Serilog & Elasticsearch

- Configured in appsettings.json under `Serilog` section
- Logs sent to Elasticsearch at `http://localhost:9200`
- Index format: `kantar-api-logs-{yyyy.MM.dd}`
- User context automatically added to logs via [UserContextMiddleware.cs](Kantarv2/Middleware/UserContextMiddleware.cs)
- Enrichers: Environment, Thread
- Console and Elasticsearch sinks enabled

### Caching with Redis

- Redis connection: `localhost:6379`
- Instance name prefix: `Kantarv2_`
- Configured in [Program.cs](Kantarv2/Program.cs:57-63)
- Both `IDistributedCache` (StackExchange) and `IConnectionMultiplexer` available for injection

### LLM Integration

[Services/LlmService.cs](Kantarv2/Services/LlmService.cs) provides LLM capabilities:
- Uses Groq API with `llama-3.1-8b-instant` model
- Endpoint: `https://api.groq.com/openai/v1/chat/completions`
- Handles chat message history (List<object>)
- API key currently hardcoded (should be moved to configuration)

Used by [LlmController.cs](Kantarv2/Controllers/LlmController.cs) via [Llmqueryhandler.cs](Kantarv2/Handler/QueryHandler/Llmqueryhandler.cs)

### Excel Export Service

[Services/ExcelService.cs](Kantarv2/Services/ExcelService.cs) uses ClosedXML:
- Implements `IExcelServiceInterface`
- Generates Excel files from `ExportExcelDto`
- Returns byte arrays for download

## Project Structure

```
Kantarv2/
├── Command/              # Write operations (CQRS commands)
│   ├── Product/
│   ├── UnitPrice/
│   └── User/
├── Queries/              # Read operations (CQRS queries)
│   ├── Llm/
│   ├── Products/
│   ├── UnitPrice/
│   └── User/
├── Handler/              # MediatR handlers
│   ├── CommandHandler/   # Command handlers
│   └── QueryHandler/     # Query handlers
├── Controllers/          # API endpoints
├── DAL/                  # Database context
├── Entities/             # Domain models
├── Dtos/                 # Data transfer objects
├── Services/             # Business services
├── Middleware/           # Custom middleware
├── Migrations/           # EF migrations
├── Enums/                # Enumerations
└── Pagination/           # Pagination helpers
```

## Configuration Requirements

### appsettings.json

Required configuration sections:
- `ConnectionStrings:DefaultConnection` - PostgreSQL connection
- `Appsettings:Token` - JWT secret key (min 256 bits)
- `Appsettings:Issuer` - JWT issuer
- `Appsettings:Audience` - JWT audience
- `Serilog` - Elasticsearch logging configuration
- `Gemini:ApiKey` - (Present but usage unclear)

### External Dependencies

Required running services:
- **PostgreSQL**: `localhost:5432`, database: `kantar`
- **Redis**: `localhost:6379`
- **Elasticsearch**: `http://localhost:9200`
- **Groq API**: Internet connection for LLM queries

## API Documentation

- OpenAPI/Swagger available in development mode
- Scalar API reference UI at `/scalar/v1` (via Scalar.AspNetCore package)
- Base controller: [BaseController.cs](Kantarv2/Controllers/BaseController.cs) provides MediatR mediator injection

## Identity Configuration

[Program.cs](Kantarv2/Program.cs:31-50) configures ASP.NET Core Identity:
- Password requirements: Minimal (6 chars, no complexity requirements)
- Unique email required
- Email/phone confirmation disabled
- Uses integer IDs (`User`, `IdentityRole<int>`)
- Soft delete via `IsDeleted` flag on User entity

## Key Patterns to Follow

1. **New features**: Create Command/Query → Handler → Controller endpoint
2. **Database changes**: Add migration → Update KantarDbContext → Run `dotnet ef database update`
3. **Authentication**: All commands/queries return `Response<T>`, use StatusCode for HTTP responses
4. **Logging**: User context automatically enriched, use ILogger injection
5. **Commands must return Response<T>**: Never return raw types from handlers
