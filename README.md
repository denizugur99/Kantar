# Kantarv2

## Project Overview

Kantarv2 is a modern enterprise-grade ASP.NET Core 10.0 Web API built with CQRS pattern and asynchronous message processing. The system provides comprehensive product and unit price management with advanced features including JWT-based authentication, real-time Excel export capabilities via SignalR, LLM integration for intelligent data processing, and distributed caching. Built for scalability and maintainability, it leverages PostgreSQL for data persistence, RabbitMQ for message brokering, Redis for caching, and AWS S3 for file storage.

## Features

- **CQRS Architecture** - Clean separation of Commands (writes) and Queries (reads) using MediatR
- **JWT Authentication** - Secure token-based authentication with SecurityStamp validation
- **Role-Based Authorization** - Three-tier role system (SuperAdmin, Admin, User)
- **Async Excel Export** - Background Excel generation with real-time notifications via SignalR
- **LLM Integration** - Groq API integration for AI-powered data analysis
- **Distributed Caching** - Redis-based caching for improved performance
- **Message Queue** - RabbitMQ integration with MassTransit for background processing
- **Cloud Storage** - AWS S3 integration for file storage with pre-signed URLs
- **Email Notifications** - SMTP-based email service for user notifications
- **Structured Logging** - Serilog with Grafana Loki integration
- **API Documentation** - Interactive Scalar UI at `/scalar/v1`
- **Health Checks** - Built-in health check endpoints

## Tech Stack

### Backend Framework
- **ASP.NET Core 10.0** - Modern web API framework
- **.NET 10.0** - Latest .NET runtime

### Architecture & Patterns
- **MediatR** - CQRS and Mediator pattern implementation
- **AutoMapper** - Object-to-object mapping
- **Entity Framework Core 10.0** - ORM for database operations

### Authentication & Authorization
- **ASP.NET Core Identity** - User management with `Guid` primary keys
- **JWT Bearer** - Token-based authentication
- **Microsoft.IdentityModel.JsonWebTokens** - JWT token handling

### Databases & Caching
- **PostgreSQL** - Primary relational database (Npgsql provider)
- **Redis** - Distributed caching (StackExchange.Redis)

### Message Broker
- **RabbitMQ** - Message queue for async operations
- **MassTransit** - Message bus abstraction layer

### File Processing & Storage
- **ClosedXML** - Excel file generation
- **AWS S3** - Cloud file storage

### External APIs
- **Groq API** - LLM queries (llama-3.1-8b-instant model)
- **OllamaSharp** - Local LLM integration support

### Logging & Monitoring
- **Serilog** - Structured logging
- **Grafana Loki** - Log aggregation and querying

### API Documentation
- **Scalar** - Interactive API documentation UI
- **OpenAPI** - API specification

### Real-time Communication
- **SignalR** - Real-time web functionality for Excel export notifications

## Architecture

### CQRS Pattern

All operations are split into Commands (writes) and Queries (reads), each with a dedicated handler:

```
Controller → _mediator.Send(Command/Query) → Handler → Response<T> → BaseController.CreateActionResultInstance()
```

Controllers inherit `BaseController` which provides `CreateActionResultInstance<T>()` to convert `Response<T>` into HTTP responses. Controllers inject `IMediator` directly.

### Request Flow

1. Client sends HTTP request to Controller
2. Controller creates a Command or Query object
3. Controller sends it via `_mediator.Send()`
4. MediatR routes to appropriate Handler
5. Handler processes the request and returns `Response<T>`
6. `BaseController.CreateActionResultInstance()` converts to HTTP response

### Response Pattern

All handlers return `Response<T>` (defined in `Dtos/Response.cs`). Use the static factory methods:

```csharp
// Success responses
Response<T>.Success(statusCode, data, pagination?)
Response<T>.Success(statusCode)

// Error responses
Response<T>.Fail(statusCode, errorMessage)
Response<T>.Fail(statusCode, errorList)
```

`StatusCode` and `IsSuccess` are `[JsonIgnore]`; `Data`, `Errors`, and `Pagination` are conditionally serialized.

### Async Messaging with MassTransit/RabbitMQ

Background processing uses MassTransit consumers located in `Consumers/`:

- **ExcelExportConsumer** - Generates Excel files via ClosedXML, uploads to S3, notifies via SignalR
- **UserCreatedConsumer** - Handles post-registration logic
- **PasswordResetEmailConsumer** - Sends password reset emails

Message contracts are defined in `Messages/`. Publish messages via `IPublishEndpoint` or `IBus`.

### Excel Export Workflow

This is a core async feature of the system:

1. Client calls export endpoint → Handler publishes `ExcelExportMessage` to RabbitMQ → returns HTTP 202 (Accepted)
2. `ExcelExportConsumer` picks up message, generates Excel, uploads to S3 (`excel/{userId}/{correlationId}.xlsx`)
3. SignalR hub (`/hubs/excel-export`) sends `ExcelExportCompleted` event to user's group with pre-signed S3 URL (30-min expiry)
4. Client downloads file via the pre-signed S3 URL or the download endpoint with correlationId

### Authentication & Authorization

- **JWT Bearer** tokens with SecurityStamp validation on every request
- `Program.cs` OnTokenValidated event validates:
  - User exists and is not soft-deleted
  - `security_stamp` claim matches database value
- Logout/password change invalidates all existing tokens by updating SecurityStamp
- SignalR tokens extracted from `?access_token=` query string for WebSocket connections

Three seeded roles:
- `SuperAdmin` - Full system access
- `Admin` - Product and unit price management
- `User` - Read-only access

Identity uses `Guid` IDs (`User` extends `IdentityUser<Guid>`, roles use `IdentityRole<Guid>`).

### Middleware Pipeline Order

```
UseHttpsRedirection → UseAuthentication → UserContextMiddleware → UseAuthorization → MapControllers → MapHub
```

`UserContextMiddleware` enriches Serilog log context with UserId, UserName, Email, and Roles from authenticated user's claims. It must remain after `UseAuthentication()`.

## Prerequisites

### Required Software
- **.NET 10.0 SDK** - [Download](https://dotnet.microsoft.com/download/dotnet/10.0)
- **Docker & Docker Compose** - [Download](https://www.docker.com/products/docker-desktop)
- **PostgreSQL** (via Docker or local installation)
- **Redis** (via Docker or local installation)
- **RabbitMQ** (via Docker or local installation)

### External Services
- **AWS Account** - For S3 file storage (Access Key, Secret Key, Bucket Name, Region)
- **Groq API Key** - For LLM integration ([Get API Key](https://console.groq.com))
- **SMTP Server** - For email notifications (Gmail SMTP recommended)
- **Grafana Loki** (optional) - For log aggregation

## Getting Started

### 1. Clone the Repository

```bash
git clone https://github.com/denizugur99/Kantarv2.git
cd Kantarv2
```

### 2. Start Infrastructure Services

Use Docker Compose to start PostgreSQL, Redis, RabbitMQ, and Grafana Loki:

```bash
docker compose up -d
```

This will start:
- **PostgreSQL** on port 5433 (container port 5432)
- **Redis** on port 6380 (container port 6379)
- **RabbitMQ** on ports 5672 (AMQP) and 15672 (management UI)
- **Grafana Loki** on port 3100

### 3. Configure Application Settings

Create `appsettings.Development.json` in the `Kantarv2` project directory (see Configuration section below for full structure).

**Important:** The configuration key is `AppSettings` (not `Appsettings`).

### 4. Apply Database Migrations

Migrations are auto-applied on startup (`Program.cs` calls `MigrateAsync`), but you can also run manually:

```bash
# Create a new migration
dotnet ef migrations add MigrationName --project Kantarv2

# Apply migrations
dotnet ef database update --project Kantarv2
```

### 5. Run the Application

```bash
# Build the project
dotnet build

# Run the API
dotnet run --project Kantarv2/Kantarv2.csproj
```

Or run everything in Docker:

```bash
docker compose up --build
```

### 6. Access the API

- **API Base URL:** `https://localhost:5001/api` (or your configured port)
- **Scalar API Documentation:** `https://localhost:5001/scalar/v1`
- **Health Check:** `GET https://localhost:5001/health`
- **RabbitMQ Management UI:** `http://localhost:15672` (guest/guest)

## Configuration

### appsettings.json Structure

```json
{
  "AppSettings": {
    "Token": "your-256-bit-secret-key-here-minimum-32-characters",
    "Issuer": "https://your-domain.com",
    "Audience": "https://your-domain.com"
  },
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5433;Database=kantarv2;Username=postgres;Password=postgres",
    "Redis": "localhost:6380"
  },
  "RabbitMQ": {
    "Uri": "amqp://guest:guest@localhost:5672"
  },
  "Groq": {
    "ApiKey": "your-groq-api-key",
    "Endpoint": "https://api.groq.com/openai/v1/chat/completions",
    "Model": "llama-3.1-8b-instant"
  },
  "AWS": {
    "S3": {
      "AccessKey": "your-aws-access-key",
      "SecretKey": "your-aws-secret-key",
      "BucketName": "your-bucket-name",
      "Region": "us-east-1"
    }
  },
  "EmailSettings": {
    "SmtpServer": "smtp.gmail.com",
    "SmtpPort": 587,
    "SenderEmail": "your-email@gmail.com",
    "Password": "your-app-password",
    "EnableSsl": true
  },
  "Serilog": {
    "Using": ["Serilog.Sinks.Grafana.Loki"],
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning"
      }
    },
    "WriteTo": [
      {
        "Name": "GrafanaLoki",
        "Args": {
          "uri": "http://localhost:3100",
          "labels": [
            {
              "key": "app",
              "value": "kantarv2"
            }
          ]
        }
      }
    ]
  }
}
```

### Configuration Notes

- **JWT Secret**: Must be at least 256 bits (32 characters)
- **PostgreSQL Port**: Docker Compose maps to 5433 on host, 5432 in container
- **Redis Port**: Docker Compose maps to 6380 on host, 6379 in container
- **Gmail SMTP**: Use App Password, not regular password ([Generate App Password](https://support.google.com/accounts/answer/185833))

## Key Services

| Service | Interface | Registration | Purpose |
|---------|-----------|-------------|---------|
| TokenService | ITokenServiceInterface | Scoped | JWT + refresh token generation |
| ExcelService | IExcelServiceInterface | Scoped | ClosedXML Excel generation |
| LlmService | ILlmService | HttpClient (Scoped) | Groq API integration (llama-3.1-8b-instant) |
| S3Service | IS3Service | Singleton | AWS S3 upload + pre-signed URLs |
| EmailService | IEmailService | Scoped | Gmail SMTP email sending |
| RabbitMQService | IRabbitMQService | Singleton | Direct RabbitMQ operations |
| RoleSeeder | RoleSeeder | Scoped | Seeds SuperAdmin/Admin/User roles on startup |

## API Documentation

### Interactive Documentation

Access the Scalar API documentation UI:

```
https://localhost:5001/scalar/v1
```

### Endpoint Categories

- **Authentication** - Login, logout, token refresh, password reset
- **User Management** - User CRUD operations (SuperAdmin only)
- **Products** - Product management (Admin/SuperAdmin)
- **Unit Prices** - Unit price management (Admin/SuperAdmin)
- **Excel Export** - Async Excel generation and download
- **LLM** - AI-powered data analysis
- **SignalR Hub** - `/hubs/excel-export` for real-time notifications

For detailed endpoint documentation, see [API_DOCUMENTATION.md](API_DOCUMENTATION.md).

## Development

### Adding a New Feature

1. **Create a Command or Query**

   In `Command/` or `Queries/`:

   ```csharp
   public class CreateProductCommand : IRequest<Response<ProductDto>>
   {
       public string Name { get; set; }
       public decimal Price { get; set; }
   }
   ```

2. **Create a Handler**

   In `Handler/CommandHandler/` or `Handler/QueryHandler/`:

   ```csharp
   public class CreateProductCommandHandler
       : IRequestHandler<CreateProductCommand, Response<ProductDto>>
   {
       public async Task<Response<ProductDto>> Handle(
           CreateProductCommand request,
           CancellationToken cancellationToken)
       {
           // Your logic here
           return Response<ProductDto>.Success(201, productDto);
       }
   }
   ```

3. **Add Controller Endpoint**

   ```csharp
   [HttpPost]
   public async Task<IActionResult> CreateProduct(CreateProductCommand command)
   {
       var response = await _mediator.Send(command);
       return CreateActionResultInstance(response);
   }
   ```

### Publishing Messages

```csharp
// Inject IPublishEndpoint or IBus
await _publishEndpoint.Publish(new ExcelExportMessage
{
    UserId = userId,
    CorrelationId = correlationId,
    Data = exportData
});
```

### Directory Structure

```
Kantarv2/
├── Command/              # CQRS Commands
├── Queries/             # CQRS Queries
├── Handler/
│   ├── CommandHandler/  # Command handlers
│   └── QueryHandler/    # Query handlers
├── Controllers/         # API Controllers
├── Consumers/          # MassTransit consumers
├── Messages/           # Message contracts
├── Dtos/              # Data Transfer Objects
├── Models/            # Domain models
├── Services/          # Business services
├── Middleware/        # Custom middleware
└── Program.cs         # Application entry point
```

---

# Kantarv2 (Türkçe)

## Proje Genel Bakış

Kantarv2, CQRS pattern ve asenkron mesaj işleme ile geliştirilmiş modern kurumsal düzeyde bir ASP.NET Core 10.0 Web API'sidir. Sistem, JWT tabanlı kimlik doğrulama, SignalR üzerinden gerçek zamanlı Excel export yetenekleri, akıllı veri işleme için LLM entegrasyonu ve dağıtık önbellekleme gibi gelişmiş özelliklerle kapsamlı ürün ve birim fiyat yönetimi sağlar. Ölçeklenebilirlik ve sürdürülebilirlik için tasarlanmış olup, veri kalıcılığı için PostgreSQL, mesaj aracılığı için RabbitMQ, önbellekleme için Redis ve dosya depolama için AWS S3 kullanır.

## Özellikler

- **CQRS Mimarisi** - MediatR kullanarak Komutlar (yazma) ve Sorgular (okuma) ayrımı
- **JWT Kimlik Doğrulama** - SecurityStamp doğrulamalı güvenli token tabanlı kimlik doğrulama
- **Rol Tabanlı Yetkilendirme** - Üç katmanlı rol sistemi (SuperAdmin, Admin, User)
- **Asenkron Excel Export** - SignalR ile gerçek zamanlı bildirimlerle arka planda Excel oluşturma
- **LLM Entegrasyonu** - Yapay zeka destekli veri analizi için Groq API entegrasyonu
- **Dağıtık Önbellekleme** - Geliştirilmiş performans için Redis tabanlı önbellekleme
- **Mesaj Kuyruğu** - Arka plan işleme için MassTransit ile RabbitMQ entegrasyonu
- **Bulut Depolama** - Önceden imzalanmış URL'lerle dosya depolama için AWS S3 entegrasyonu
- **E-posta Bildirimleri** - Kullanıcı bildirimleri için SMTP tabanlı e-posta servisi
- **Yapılandırılmış Loglama** - Grafana Loki entegrasyonlu Serilog
- **API Dokümantasyonu** - `/scalar/v1` adresinde interaktif Scalar UI
- **Sağlık Kontrolleri** - Yerleşik sağlık kontrolü endpoint'leri

## Teknoloji Yığını

### Backend Framework
- **ASP.NET Core 10.0** - Modern web API framework'ü
- **.NET 10.0** - En son .NET runtime

### Mimari & Pattern'ler
- **MediatR** - CQRS ve Mediator pattern implementasyonu
- **AutoMapper** - Nesne-nesne eşleme
- **Entity Framework Core 10.0** - Veritabanı işlemleri için ORM

### Kimlik Doğrulama & Yetkilendirme
- **ASP.NET Core Identity** - `Guid` primary key'li kullanıcı yönetimi
- **JWT Bearer** - Token tabanlı kimlik doğrulama
- **Microsoft.IdentityModel.JsonWebTokens** - JWT token işleme

### Veritabanları & Önbellekleme
- **PostgreSQL** - Birincil ilişkisel veritabanı (Npgsql sağlayıcı)
- **Redis** - Dağıtık önbellekleme (StackExchange.Redis)

### Mesaj Aracısı
- **RabbitMQ** - Asenkron işlemler için mesaj kuyruğu
- **MassTransit** - Mesaj bus soyutlama katmanı

### Dosya İşleme & Depolama
- **ClosedXML** - Excel dosya oluşturma
- **AWS S3** - Bulut dosya depolama

### Harici API'ler
- **Groq API** - LLM sorguları (llama-3.1-8b-instant modeli)
- **OllamaSharp** - Yerel LLM entegrasyon desteği

### Loglama & İzleme
- **Serilog** - Yapılandırılmış loglama
- **Grafana Loki** - Log toplama ve sorgulama

### API Dokümantasyonu
- **Scalar** - İnteraktif API dokümantasyon UI
- **OpenAPI** - API spesifikasyonu

### Gerçek Zamanlı İletişim
- **SignalR** - Excel export bildirimleri için gerçek zamanlı web fonksiyonalitesi

## Mimari

### CQRS Pattern

Tüm işlemler, her biri özel bir handler'a sahip Komutlar (yazma) ve Sorgular (okuma) olarak ayrılmıştır:

```
Controller → _mediator.Send(Command/Query) → Handler → Response<T> → BaseController.CreateActionResultInstance()
```

Controller'lar, `Response<T>` tipini HTTP yanıtlarına dönüştüren `CreateActionResultInstance<T>()` sağlayan `BaseController`'dan türer. Controller'lar `IMediator`'ı doğrudan enjekte eder.

### İstek Akışı

1. İstemci Controller'a HTTP isteği gönderir
2. Controller bir Command veya Query nesnesi oluşturur
3. Controller bunu `_mediator.Send()` ile gönderir
4. MediatR uygun Handler'a yönlendirir
5. Handler isteği işler ve `Response<T>` döner
6. `BaseController.CreateActionResultInstance()` HTTP yanıtına dönüştürür

### Response Pattern

Tüm handler'lar `Response<T>` döner (`Dtos/Response.cs` içinde tanımlı). Statik fabrika metotlarını kullanın:

```csharp
// Başarı yanıtları
Response<T>.Success(statusCode, data, pagination?)
Response<T>.Success(statusCode)

// Hata yanıtları
Response<T>.Fail(statusCode, errorMessage)
Response<T>.Fail(statusCode, errorList)
```

`StatusCode` ve `IsSuccess` `[JsonIgnore]`; `Data`, `Errors` ve `Pagination` koşullu olarak serileştirilir.

### MassTransit/RabbitMQ ile Asenkron Mesajlaşma

Arka plan işleme, `Consumers/` dizininde bulunan MassTransit consumer'ları kullanır:

- **ExcelExportConsumer** - ClosedXML ile Excel dosyaları oluşturur, S3'e yükler, SignalR ile bildirir
- **UserCreatedConsumer** - Kayıt sonrası işlemleri yönetir
- **PasswordResetEmailConsumer** - Şifre sıfırlama e-postaları gönderir

Mesaj sözleşmeleri `Messages/` içinde tanımlıdır. Mesajları `IPublishEndpoint` veya `IBus` ile yayınlayın.

### Excel Export İş Akışı

Bu, sistemin temel asenkron özelliğidir:

1. İstemci export endpoint'ini çağırır → Handler `ExcelExportMessage`'ı RabbitMQ'ya yayınlar → HTTP 202 (Accepted) döner
2. `ExcelExportConsumer` mesajı alır, Excel oluşturur, S3'e yükler (`excel/{userId}/{correlationId}.xlsx`)
3. SignalR hub'ı (`/hubs/excel-export`) kullanıcının grubuna önceden imzalanmış S3 URL (30 dakika geçerlilik) ile `ExcelExportCompleted` olayı gönderir
4. İstemci dosyayı önceden imzalanmış S3 URL veya correlationId ile download endpoint'i üzerinden indirir

### Kimlik Doğrulama & Yetkilendirme

- Her istekte SecurityStamp doğrulamalı **JWT Bearer** token'ları
- `Program.cs` OnTokenValidated olayı doğrular:
  - Kullanıcı mevcut ve soft-delete edilmemiş
  - `security_stamp` claim'i veritabanı değeri ile eşleşiyor
- Çıkış/şifre değişikliği SecurityStamp'i güncelleyerek mevcut tüm token'ları geçersiz kılar
- SignalR token'ları WebSocket bağlantıları için `?access_token=` query string'inden çıkarılır

Üç önceden oluşturulmuş rol:
- `SuperAdmin` - Tam sistem erişimi
- `Admin` - Ürün ve birim fiyat yönetimi
- `User` - Salt okunur erişim

Identity, `Guid` ID'leri kullanır (`User`, `IdentityUser<Guid>`'den türer, roller `IdentityRole<Guid>` kullanır).

### Middleware Pipeline Sırası

```
UseHttpsRedirection → UseAuthentication → UserContextMiddleware → UseAuthorization → MapControllers → MapHub
```

`UserContextMiddleware`, kimliği doğrulanmış kullanıcının claim'lerinden UserId, UserName, Email ve Roles ile Serilog log context'ini zenginleştirir. `UseAuthentication()` sonrasında kalmalıdır.

## Gereksinimler

### Gerekli Yazılımlar
- **.NET 10.0 SDK** - [İndir](https://dotnet.microsoft.com/download/dotnet/10.0)
- **Docker & Docker Compose** - [İndir](https://www.docker.com/products/docker-desktop)
- **PostgreSQL** (Docker veya yerel kurulum)
- **Redis** (Docker veya yerel kurulum)
- **RabbitMQ** (Docker veya yerel kurulum)

### Harici Servisler
- **AWS Hesabı** - S3 dosya depolama için (Access Key, Secret Key, Bucket Name, Region)
- **Groq API Key** - LLM entegrasyonu için ([API Key Al](https://console.groq.com))
- **SMTP Sunucusu** - E-posta bildirimleri için (Gmail SMTP önerilir)
- **Grafana Loki** (opsiyonel) - Log toplama için

## Başlangıç

### 1. Repository'yi Klonlayın

```bash
git clone https://github.com/denizugur99/Kantarv2.git
cd Kantarv2
```

### 2. Altyapı Servislerini Başlatın

PostgreSQL, Redis, RabbitMQ ve Grafana Loki'yi başlatmak için Docker Compose kullanın:

```bash
docker compose up -d
```

Bu şunları başlatacak:
- **PostgreSQL** 5433 portunda (container port 5432)
- **Redis** 6380 portunda (container port 6379)
- **RabbitMQ** 5672 (AMQP) ve 15672 (yönetim UI) portlarında
- **Grafana Loki** 3100 portunda

### 3. Uygulama Ayarlarını Yapılandırın

`Kantarv2` proje dizininde `appsettings.Development.json` oluşturun (tam yapı için aşağıdaki Yapılandırma bölümüne bakın).

**Önemli:** Yapılandırma anahtarı `AppSettings`'tir (`Appsettings` değil).

### 4. Veritabanı Migration'larını Uygulayın

Migration'lar başlangıçta otomatik uygulanır (`Program.cs`, `MigrateAsync` çağırır), ancak manuel olarak da çalıştırabilirsiniz:

```bash
# Yeni migration oluştur
dotnet ef migrations add MigrationName --project Kantarv2

# Migration'ları uygula
dotnet ef database update --project Kantarv2
```

### 5. Uygulamayı Çalıştırın

```bash
# Projeyi derle
dotnet build

# API'yi çalıştır
dotnet run --project Kantarv2/Kantarv2.csproj
```

Veya her şeyi Docker'da çalıştırın:

```bash
docker compose up --build
```

### 6. API'ye Erişin

- **API Base URL:** `https://localhost:5001/api` (veya yapılandırılmış port)
- **Scalar API Dokümantasyonu:** `https://localhost:5001/scalar/v1`
- **Sağlık Kontrolü:** `GET https://localhost:5001/health`
- **RabbitMQ Yönetim UI:** `http://localhost:15672` (guest/guest)

## Yapılandırma

### appsettings.json Yapısı

```json
{
  "AppSettings": {
    "Token": "your-256-bit-secret-key-here-minimum-32-characters",
    "Issuer": "https://your-domain.com",
    "Audience": "https://your-domain.com"
  },
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5433;Database=kantarv2;Username=postgres;Password=postgres",
    "Redis": "localhost:6380"
  },
  "RabbitMQ": {
    "Uri": "amqp://guest:guest@localhost:5672"
  },
  "Groq": {
    "ApiKey": "your-groq-api-key",
    "Endpoint": "https://api.groq.com/openai/v1/chat/completions",
    "Model": "llama-3.1-8b-instant"
  },
  "AWS": {
    "S3": {
      "AccessKey": "your-aws-access-key",
      "SecretKey": "your-aws-secret-key",
      "BucketName": "your-bucket-name",
      "Region": "us-east-1"
    }
  },
  "EmailSettings": {
    "SmtpServer": "smtp.gmail.com",
    "SmtpPort": 587,
    "SenderEmail": "your-email@gmail.com",
    "Password": "your-app-password",
    "EnableSsl": true
  },
  "Serilog": {
    "Using": ["Serilog.Sinks.Grafana.Loki"],
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning"
      }
    },
    "WriteTo": [
      {
        "Name": "GrafanaLoki",
        "Args": {
          "uri": "http://localhost:3100",
          "labels": [
            {
              "key": "app",
              "value": "kantarv2"
            }
          ]
        }
      }
    ]
  }
}
```

### Yapılandırma Notları

- **JWT Secret**: En az 256 bit (32 karakter) olmalıdır
- **PostgreSQL Port**: Docker Compose host'ta 5433'e, container'da 5432'ye map'ler
- **Redis Port**: Docker Compose host'ta 6380'e, container'da 6379'a map'ler
- **Gmail SMTP**: Normal şifre değil, Uygulama Şifresi kullanın ([Uygulama Şifresi Oluştur](https://support.google.com/accounts/answer/185833))

## Anahtar Servisler

| Servis | Interface | Kayıt | Amaç |
|---------|-----------|-------------|---------|
| TokenService | ITokenServiceInterface | Scoped | JWT + refresh token oluşturma |
| ExcelService | IExcelServiceInterface | Scoped | ClosedXML Excel oluşturma |
| LlmService | ILlmService | HttpClient (Scoped) | Groq API entegrasyonu (llama-3.1-8b-instant) |
| S3Service | IS3Service | Singleton | AWS S3 yükleme + önceden imzalanmış URL'ler |
| EmailService | IEmailService | Scoped | Gmail SMTP e-posta gönderme |
| RabbitMQService | IRabbitMQService | Singleton | Doğrudan RabbitMQ işlemleri |
| RoleSeeder | RoleSeeder | Scoped | Başlangıçta SuperAdmin/Admin/User rollerini oluşturur |

## API Dokümantasyonu

### İnteraktif Dokümantasyon

Scalar API dokümantasyon UI'sine erişin:

```
https://localhost:5001/scalar/v1
```

### Endpoint Kategorileri

- **Kimlik Doğrulama** - Giriş, çıkış, token yenileme, şifre sıfırlama
- **Kullanıcı Yönetimi** - Kullanıcı CRUD işlemleri (yalnızca SuperAdmin)
- **Ürünler** - Ürün yönetimi (Admin/SuperAdmin)
- **Birim Fiyatlar** - Birim fiyat yönetimi (Admin/SuperAdmin)
- **Excel Export** - Asenkron Excel oluşturma ve indirme
- **LLM** - Yapay zeka destekli veri analizi
- **SignalR Hub** - Gerçek zamanlı bildirimler için `/hubs/excel-export`

Detaylı endpoint dokümantasyonu için [API_DOCUMENTATION.md](API_DOCUMENTATION.md) dosyasına bakın.

## Geliştirme

### Yeni Özellik Ekleme

1. **Bir Command veya Query Oluşturun**

   `Command/` veya `Queries/` içinde:

   ```csharp
   public class CreateProductCommand : IRequest<Response<ProductDto>>
   {
       public string Name { get; set; }
       public decimal Price { get; set; }
   }
   ```

2. **Bir Handler Oluşturun**

   `Handler/CommandHandler/` veya `Handler/QueryHandler/` içinde:

   ```csharp
   public class CreateProductCommandHandler
       : IRequestHandler<CreateProductCommand, Response<ProductDto>>
   {
       public async Task<Response<ProductDto>> Handle(
           CreateProductCommand request,
           CancellationToken cancellationToken)
       {
           // İşlem mantığınız
           return Response<ProductDto>.Success(201, productDto);
       }
   }
   ```

3. **Controller Endpoint'i Ekleyin**

   ```csharp
   [HttpPost]
   public async Task<IActionResult> CreateProduct(CreateProductCommand command)
   {
       var response = await _mediator.Send(command);
       return CreateActionResultInstance(response);
   }
   ```

### Mesaj Yayınlama

```csharp
// IPublishEndpoint veya IBus enjekte edin
await _publishEndpoint.Publish(new ExcelExportMessage
{
    UserId = userId,
    CorrelationId = correlationId,
    Data = exportData
});
```

### Dizin Yapısı

```
Kantarv2/
├── Command/              # CQRS Komutları
├── Queries/             # CQRS Sorguları
├── Handler/
│   ├── CommandHandler/  # Komut handler'ları
│   └── QueryHandler/    # Sorgu handler'ları
├── Controllers/         # API Controller'ları
├── Consumers/          # MassTransit consumer'ları
├── Messages/           # Mesaj sözleşmeleri
├── Dtos/              # Veri Transfer Nesneleri
├── Models/            # Domain modelleri
├── Services/          # İş servisleri
├── Middleware/        # Özel middleware'ler
└── Program.cs         # Uygulama giriş noktası
```
