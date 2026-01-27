# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build and Run Commands

```bash
# Build
dotnet build

# Run the API
dotnet run --project Kantarv2/Kantarv2.csproj

# Start infrastructure (PostgreSQL, Redis, RabbitMQ, Loki)
docker compose up -d

# Run everything including the API in containers
docker compose up --build

# EF Core migrations
dotnet ef migrations add MigrationName --project Kantarv2
dotnet ef database update --project Kantarv2
```

Note: Migrations are auto-applied on startup (`Program.cs` calls `MigrateAsync`). Docker Compose maps PostgreSQL to port 5433, Redis to 6380, and RabbitMQ to 5672/15672 on the host.

## Architecture

ASP.NET Core 10.0 Web API using CQRS with MediatR. All operations are split into Commands (writes) and Queries (reads), each with a dedicated handler.

### Request Flow

```
Controller → _mediator.Send(Command/Query) → Handler → Response<T> → BaseController.CreateActionResultInstance()
```

Controllers inherit `BaseController` which provides `CreateActionResultInstance<T>()` to convert `Response<T>` into HTTP responses. Controllers inject `IMediator` directly (BaseController does not provide it).

### Adding a New Feature

1. Create a Command or Query class in `Command/` or `Queries/` implementing `IRequest<Response<T>>`
2. Create a handler in `Handler/CommandHandler/` or `Handler/QueryHandler/` implementing `IRequestHandler<TRequest, Response<T>>`
3. Add a controller endpoint that sends the command/query via `_mediator.Send()`

### Response Pattern

All handlers return `Response<T>` (`Dtos/Response.cs`). Use the static factory methods:
- `Response<T>.Success(statusCode, data, pagination?)` or `Response<T>.Success(statusCode)`
- `Response<T>.Fail(statusCode, errorMessage)` or `Response<T>.Fail(statusCode, errorList)`

`StatusCode` and `IsSuccess` are `[JsonIgnore]`; `Data`, `Errors`, and `Pagination` are conditionally serialized.

### Async Messaging with MassTransit/RabbitMQ

Background processing uses MassTransit consumers (`Consumers/`):
- `ExcelExportConsumer` - Generates Excel via ClosedXML, uploads to S3, notifies via SignalR
- `UserCreatedConsumer` - Handles post-registration logic
- `PasswordResetEmailConsumer` - Sends password reset emails

Message contracts are in `Messages/`. Publish messages via `IPublishEndpoint` or `IBus`.

### Excel Export Workflow

This is a core async feature:
1. Client calls export endpoint → Handler publishes `ExcelExportMessage` to RabbitMQ → returns 202
2. `ExcelExportConsumer` generates Excel, uploads to S3 (`excel/{userId}/{correlationId}.xlsx`)
3. SignalR hub (`/hubs/excel-export`) sends `ExcelExportCompleted` event to user's group with pre-signed S3 URL (30-min expiry)
4. Client downloads via the S3 URL or the download endpoint with correlationId

### Authentication

JWT Bearer with SecurityStamp validation on every request (`Program.cs` OnTokenValidated event). The middleware validates the user exists, is not soft-deleted, and the `security_stamp` claim matches the database value. Logout/password change invalidates all existing tokens by updating SecurityStamp.

SignalR tokens are extracted from `?access_token=` query string for WebSocket connections.

Three seeded roles: `SuperAdmin`, `Admin`, `User`. Identity uses `Guid` IDs (`User` extends `IdentityUser<Guid>`, roles use `IdentityRole<Guid>`).

### Middleware Pipeline Order

```
UseHttpsRedirection → UseAuthentication → UserContextMiddleware → UseAuthorization → MapControllers → MapHub
```

`UserContextMiddleware` enriches Serilog log context with UserId, UserName, Email, and Roles from the authenticated user's claims. It must remain after `UseAuthentication()`.

## Key Services

| Service | Interface | Registration | Purpose |
|---------|-----------|-------------|---------|
| TokenService | ITokenServiceInterface | Scoped | JWT + refresh token generation |
| ExcelService | IExcelServiceInterface | Scoped | ClosedXML Excel generation |
| LlmService | ILlmService | HttpClient (Scoped) | Groq API (llama-3.1-8b-instant) |
| S3Service | IS3Service | Singleton | AWS S3 upload + pre-signed URLs |
| EmailService | IEmailService | Scoped | Gmail SMTP email sending |
| RabbitMQService | IRabbitMQService | Singleton | Direct RabbitMQ operations |
| RoleSeeder | RoleSeeder | Scoped | Seeds SuperAdmin/Admin/User roles on startup |

## Configuration

Configuration key is `AppSettings` (not `Appsettings`):
- `AppSettings:Token` - JWT secret (min 256 bits)
- `AppSettings:Issuer` / `AppSettings:Audience` - JWT validation
- `ConnectionStrings:DefaultConnection` - PostgreSQL
- `ConnectionStrings:Redis` - Redis
- `RabbitMQ:Uri` - RabbitMQ AMQP URI
- `Groq:ApiKey`, `Groq:Endpoint`, `Groq:Model` - LLM config
- `AWS:S3:AccessKey`, `AWS:S3:SecretKey`, `AWS:S3:BucketName`, `AWS:S3:Region` - S3
- `EmailSettings` - SMTP config (SmtpServer, SmtpPort, SenderEmail, Password, EnableSsl)
- `Serilog` - Grafana Loki logging config

## External Dependencies

Required running services (via `docker compose up -d`):
- **PostgreSQL** (5433:5432) - Primary database
- **Redis** (6380:6379) - Distributed cache
- **RabbitMQ** (5672 AMQP, 15672 management UI) - Message broker
- **Grafana Loki** (3100) - Log aggregation

External APIs:
- **Groq API** - LLM queries
- **AWS S3** - Excel file storage
- **Gmail SMTP** - Email delivery

## Logging

Serilog with Grafana Loki sink (not Elasticsearch). User context (UserId, UserName, Email, Roles) is automatically added to all log entries via `UserContextMiddleware`.

## API Reference

Scalar API reference UI available at `/scalar/v1`. Health check at `GET /health`.
